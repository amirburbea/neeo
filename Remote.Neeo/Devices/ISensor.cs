using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface ISensor
    {
    }

    public interface ISensor<T> : ISensor
    {
        Task<T> GetValueAsync(string deviceId);
    }

    public static class SensorValue
    {
        private static readonly ConcurrentDictionary<Type, Func<ISensor, string, Task<object?>>> _functions = new();
        private static readonly MethodInfo _getValueAsyncMethod = typeof(SensorValue).GetMethod(nameof(SensorValue.GetValueAsync), BindingFlags.Static | BindingFlags.NonPublic)!;

        public static Task<object?> GetValueAsync(this ISensor sensor, string deviceId)
        {
            Type? type = Array.Find(
                (sensor ?? throw new ArgumentNullException(nameof(sensor))).GetType().GetInterfaces(),
                type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISensor<>)
            );
            if (type == null)
            {
                throw new ArgumentException($"Type does not implement {typeof(ISensor<>).Name}.", nameof(sensor));
            }
            return SensorValue._functions.GetOrAdd(type, SensorValue.CreateFunction).Invoke(sensor, deviceId);
        }

        private static Func<ISensor, string, Task<object?>> CreateFunction(Type sensorType)
        {
            ParameterExpression sensorParameter = Expression.Parameter(typeof(ISensor));
            ParameterExpression deviceIdParameter = Expression.Parameter(typeof(string));
            Expression<Func<ISensor, string, Task<object?>>> lambdaExpression = Expression.Lambda<Func<ISensor, string, Task<object?>>>(
                Expression.Call(
                    SensorValue._getValueAsyncMethod.MakeGenericMethod(sensorType.GetGenericArguments()),
                    Expression.Convert(
                        sensorParameter,
                        sensorType
                    ),
                    deviceIdParameter
                ),
                sensorParameter,
                deviceIdParameter
            );
            return lambdaExpression.Compile();
        }

        private static async Task<object?> GetValueAsync<T>(ISensor<T> sensor, string deviceId)
        {
            return await sensor.GetValueAsync(deviceId).ConfigureAwait(false);
        }
    }
}
