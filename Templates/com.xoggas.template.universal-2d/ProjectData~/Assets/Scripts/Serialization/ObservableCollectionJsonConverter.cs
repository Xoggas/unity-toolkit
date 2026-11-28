using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ObservableCollections;

namespace Serialization
{
    /// <summary>
    /// Serializes ObservableCollections types the same way as their System.Collections counterparts:
    /// ObservableList, ObservableHashSet, ObservableQueue, ObservableStack, ObservableRingBuffer and
    /// ObservableFixedSizeRingBuffer become JSON arrays, ObservableDictionary becomes a JSON object.
    /// When the target already holds a collection instance, it is cleared and refilled instead of
    /// being replaced, so existing subscriptions and views keep working.
    /// </summary>
    public sealed class ObservableCollectionJsonConverter : JsonConverter
    {
        private static readonly Dictionary<Type, Type> HandlerTypes = new()
        {
            [typeof(ObservableList<>)] = typeof(ListHandler<>),
            [typeof(ObservableHashSet<>)] = typeof(HashSetHandler<>),
            [typeof(ObservableQueue<>)] = typeof(QueueHandler<>),
            [typeof(ObservableStack<>)] = typeof(StackHandler<>),
            [typeof(ObservableRingBuffer<>)] = typeof(RingBufferHandler<>),
            [typeof(ObservableFixedSizeRingBuffer<>)] = typeof(FixedSizeRingBufferHandler<>),
            [typeof(ObservableDictionary<,>)] = typeof(DictionaryHandler<,>),
        };

        private static readonly ConcurrentDictionary<Type, Handler> Handlers = new();

        public override bool CanConvert(Type objectType) => FindCollectionType(objectType) != null;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            GetHandler(value.GetType()).Write(writer, value, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var handler = GetHandler(objectType);
            if (existingValue != null && !objectType.IsInstanceOfType(existingValue))
            {
                existingValue = null;
            }

            return handler.Read(reader, objectType, existingValue, serializer);
        }

        private static Handler GetHandler(Type type)
        {
            return Handlers.GetOrAdd(type, static t =>
            {
                var collectionType = FindCollectionType(t)
                                     ?? throw new JsonSerializationException($"{t} is not a supported observable collection.");
                var handlerType = HandlerTypes[collectionType.GetGenericTypeDefinition()]
                    .MakeGenericType(collectionType.GetGenericArguments());
                return (Handler)Activator.CreateInstance(handlerType);
            });
        }

        private static Type FindCollectionType(Type type)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                if (t.IsGenericType && HandlerTypes.ContainsKey(t.GetGenericTypeDefinition()))
                {
                    return t;
                }
            }

            return null;
        }

        private abstract class Handler
        {
            public abstract void Write(JsonWriter writer, object collection, JsonSerializer serializer);
            public abstract object Read(JsonReader reader, Type objectType, object existing, JsonSerializer serializer);

            // Subclasses of the observable collections (if any) are created through their parameterless constructor.
            protected static TCollection Create<TCollection>(Type objectType, Func<TCollection> fallback)
            {
                return objectType == typeof(TCollection) ? fallback() : (TCollection)Activator.CreateInstance(objectType, true);
            }
        }

        private abstract class SequenceHandler<T, TCollection> : Handler where TCollection : class, IEnumerable<T>
        {
            public override void Write(JsonWriter writer, object collection, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                foreach (var item in ToArray((TCollection)collection))
                {
                    serializer.Serialize(writer, item, typeof(T));
                }

                writer.WriteEndArray();
            }

            public override object Read(JsonReader reader, Type objectType, object existing, JsonSerializer serializer)
            {
                var items = serializer.Deserialize<T[]>(reader) ?? Array.Empty<T>();
                var collection = existing as TCollection ?? Create(objectType, () => CreateNew(items.Length));
                Refill(collection, items);
                return collection;
            }

            // A snapshot avoids enumerating while another thread changes the collection.
            protected virtual T[] ToArray(TCollection collection) => collection.ToArray();
            protected abstract TCollection CreateNew(int count);
            protected abstract void Refill(TCollection collection, T[] items);
        }

        private sealed class ListHandler<T> : SequenceHandler<T, ObservableList<T>>
        {
            protected override ObservableList<T> CreateNew(int count) => new(count);

            protected override void Refill(ObservableList<T> collection, T[] items)
            {
                collection.Clear();
                collection.AddRange(items);
            }
        }

        private sealed class HashSetHandler<T> : SequenceHandler<T, ObservableHashSet<T>>
        {
            protected override ObservableHashSet<T> CreateNew(int count) => new(count);

            protected override void Refill(ObservableHashSet<T> collection, T[] items)
            {
                collection.Clear();
                collection.AddRange(items);
            }
        }

        private sealed class QueueHandler<T> : SequenceHandler<T, ObservableQueue<T>>
        {
            protected override T[] ToArray(ObservableQueue<T> collection) => collection.ToArray();
            protected override ObservableQueue<T> CreateNew(int count) => new(count);

            protected override void Refill(ObservableQueue<T> collection, T[] items)
            {
                collection.Clear();
                collection.EnqueueRange(items);
            }
        }

        // Written top-first (the order Pop returns items), so it is pushed back in reverse.
        private sealed class StackHandler<T> : SequenceHandler<T, ObservableStack<T>>
        {
            protected override T[] ToArray(ObservableStack<T> collection) => collection.ToArray();
            protected override ObservableStack<T> CreateNew(int count) => new(count);

            protected override void Refill(ObservableStack<T> collection, T[] items)
            {
                Array.Reverse(items);
                collection.Clear();
                collection.PushRange(items);
            }
        }

        private sealed class RingBufferHandler<T> : SequenceHandler<T, ObservableRingBuffer<T>>
        {
            protected override T[] ToArray(ObservableRingBuffer<T> collection) => collection.ToArray();
            protected override ObservableRingBuffer<T> CreateNew(int count) => new();

            protected override void Refill(ObservableRingBuffer<T> collection, T[] items)
            {
                collection.Clear();
                collection.AddLastRange(items);
            }
        }

        // The capacity is not stored in JSON: an existing instance keeps its own, a new one gets
        // exactly as much room as the loaded items need. Initialize the field to keep a larger capacity.
        private sealed class FixedSizeRingBufferHandler<T> : SequenceHandler<T, ObservableFixedSizeRingBuffer<T>>
        {
            protected override T[] ToArray(ObservableFixedSizeRingBuffer<T> collection) => collection.ToArray();
            protected override ObservableFixedSizeRingBuffer<T> CreateNew(int count) => new(Math.Max(count, 1));

            protected override void Refill(ObservableFixedSizeRingBuffer<T> collection, T[] items)
            {
                collection.Clear();
                collection.AddLastRange(items);
            }
        }

        private sealed class DictionaryHandler<TKey, TValue> : Handler
        {
            public override void Write(JsonWriter writer, object collection, JsonSerializer serializer)
            {
                // Newtonsoft already knows how to turn any key type into a property name.
                var snapshot = new Dictionary<TKey, TValue>((ObservableDictionary<TKey, TValue>)collection);
                serializer.Serialize(writer, snapshot);
            }

            public override object Read(JsonReader reader, Type objectType, object existing, JsonSerializer serializer)
            {
                var items = serializer.Deserialize<Dictionary<TKey, TValue>>(reader) ?? new Dictionary<TKey, TValue>();
                var collection = existing as ObservableDictionary<TKey, TValue>
                                 ?? Create(objectType, () => new ObservableDictionary<TKey, TValue>(items.Count, null));
                collection.Clear();
                foreach (var pair in items)
                {
                    collection.Add(pair.Key, pair.Value);
                }

                return collection;
            }
        }
    }
}
