using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFCoreExtensions
{
    //public class FastAccessor<T> where T : class
    //{
    //    private static readonly Hashtable _cache = new Hashtable();

    //    public static FastAccessor<T> For(T target)
    //    {
    //        if (_cache.ContainsKey(typeof(T)))
    //        {
    //            var dlg = (Action<T, string, object>)_cache[typeof(T)];
    //            return new FastAccessor<T>(dlg, target);
    //        }
    //        var dlg2 = BuildDelegate<T>();
    //        _cache[typeof(T)] = dlg2;
    //        return new FastAccessor<T>(dlg2, target);
    //    }

    //    private static Action<T, string, object> BuildDelegate<T>()
    //    {
    //        var targetType = typeof(T);
    //        var equality = typeof(string).GetMethod("op_Equality");

    //        var method = IL.NewMethod()
    //            .WithParameter<T>("target")
    //            .WithParameter<string>("name")
    //            .WithParameter<object>("value")
    //            .Returns(typeof(void));

    //        var properties = targetType.GetProperties();
    //        var labels = Enumerable.Range(0, properties.Length).Select(p => $"label{p}").ToArray();
    //        var i = 0;
    //        foreach (var prop in properties)
    //        {
    //            method = method
    //                .Ldarg("name")
    //                .Ldstr(prop.Name)
    //                .Call(equality)
    //                .Brtrue(labels[i]);
    //            i++;
    //        }
    //        method = method.Ret();
    //        i = 0;
    //        foreach (var prop in properties)
    //        {
    //            method = method
    //                .MarkLabel(labels[i])
    //                .Ldarg("target")
    //                .Ldarg("value");
    //            if (prop.PropertyType.IsValueType)
    //            {
    //                method = method.UnboxAny(prop.PropertyType);
    //            }
    //            else
    //            {
    //                method = method.Emit(OpCodes.Castclass, prop.PropertyType);
    //            }
    //            var setter = typeof(T).GetMethod("set_" + prop.Name);
    //            method = method
    //                .Callvirt(setter)
    //                .Ret();
    //            i++;
    //        }

    //        return (Action<T, string, object>)method.AsDynamicMethod.CreateDelegate(typeof(Action<T, string, object>));
    //    }

    //    private readonly Action<T, string, object> _action;
    //    private readonly T _target;

    //    private FastAccessor(Action<T, string, object> action, T target)
    //    {
    //        _action = action;
    //        _target = target;
    //    }

    //    public void Set(string name, object value)
    //    {
    //        _action.Invoke(_target, name, value);
    //    }
    //}
}
