using System;
using System.Collections.Concurrent;
using System.Reflection;
using Newtonsoft.Json;
using R3;

namespace Serialization
{
    /// <summary>
    /// Serializes R3 reactive properties (ReactiveProperty, BindableReactiveProperty,
    /// SerializableReactiveProperty and anything else derived from ReadOnlyReactiveProperty)
    /// as their plain value: <c>new ReactiveProperty&lt;int&gt;(5)</c> becomes <c>5</c>.
    /// When the target already holds a property instance, its Value is set instead of
    /// replacing the instance, so existing subscriptions keep working.
    /// </summary>
    public sealed class ReactivePropertyJsonConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, Handler> Handlers = new();

        public override bool CanConvert(Type objectType) => FindValueType(objectType) != null;

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
            return GetHandler(objectType).Read(reader, objectType, existingValue, serializer);
        }

        private static Handler GetHandler(Type type)
        {
            return Handlers.GetOrAdd(type, static t =>
            {
                var valueType = FindValueType(t)
                                ?? throw new JsonSerializationException($"{t} is not a reactive property.");
                return (Handler)Activator.CreateInstance(typeof(Handler<>).MakeGenericType(valueType));
            });
        }

        private static Type FindValueType(Type type)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ReadOnlyReactiveProperty<>))
                {
                    return t.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private abstract class Handler
        {
            public abstract void Write(JsonWriter writer, object property, JsonSerializer serializer);
            public abstract object Read(JsonReader reader, Type objectType, object existing, JsonSerializer serializer);
        }

        private sealed class Handler<T> : Handler
        {
            private readonly ConcurrentDictionary<Type, Func<T, ReactiveProperty<T>>> _factories = new();

            public override void Write(JsonWriter writer, object property, JsonSerializer serializer)
            {
                serializer.Serialize(writer, ((ReadOnlyReactiveProperty<T>)property).CurrentValue, typeof(T));
            }

            public override object Read(JsonReader reader, Type objectType, object existing, JsonSerializer serializer)
            {
                var value = reader.TokenType == JsonToken.Null ? default : serializer.Deserialize<T>(reader);

                if (existing is ReactiveProperty<T> current && objectType.IsInstanceOfType(current))
                {
                    current.Value = value;
                    return current;
                }

                return _factories.GetOrAdd(objectType, CreateFactory)(value);
            }

            private static Func<T, ReactiveProperty<T>> CreateFactory(Type objectType)
            {
                // Fields typed as ReadOnlyReactiveProperty<T> (or another abstract base) get a plain ReactiveProperty<T>.
                var concrete = objectType.IsAbstract || objectType.IsInterface ? typeof(ReactiveProperty<T>) : objectType;

                if (!typeof(ReactiveProperty<T>).IsAssignableFrom(concrete))
                {
                    throw new JsonSerializationException($"Cannot create {objectType}: it does not derive from ReactiveProperty<{typeof(T).Name}>.");
                }

                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                var withValue = concrete.GetConstructor(flags, null, new[] { typeof(T) }, null);
                if (withValue != null)
                {
                    return value => (ReactiveProperty<T>)withValue.Invoke(new object[] { value });
                }

                var parameterless = concrete.GetConstructor(flags, null, Type.EmptyTypes, null)
                                    ?? throw new JsonSerializationException($"{concrete} has no constructor taking ({typeof(T).Name}) or ().");
                return value =>
                {
                    var property = (ReactiveProperty<T>)parameterless.Invoke(null);
                    property.Value = value;
                    return property;
                };
            }
        }
    }
}
