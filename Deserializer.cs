using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

public class Deserializer
{
    XDocument _xdoc;

    public Deserializer(string fileName)
    {
        _xdoc = XDocument.Load(fileName);
    }
    public T Deserialize<T>()
    {
        var type = typeof(T);
        return type.IsGenericType ? (T)DeserializeList(type) : (T)Deserialize(type);
    }

    public object Deserialize(Type type)
    {
        var obj = type.IsInterface ? Activator.CreateInstance(GetImplement(type)) : Activator.CreateInstance(type);

        foreach(var prop in type.GetProperties())
        {
            var propType = prop.PropertyType;

            if(propType.IsClass && !propType.IsPrimitive && !propType.IsSealed && !propType.IsGenericType)
            {
                var child = Deserialize(propType);
                prop.SetValue(obj, child);
            }
            else if (propType.IsGenericType)
            {
                var child = DeserializeList(propType);
                prop.SetValue(obj, child);
            }
            else
            {
                var elem = _xdoc.Descendants().Elements().FirstOrDefault(el =>
                    el.Name.LocalName.ToLower().Equals(type.Name.ToLower()) && el.HasAttributes);

                if(elem != null)
                {
                    var attribute = elem.Attributes().FirstOrDefault(attr =>
                    attr.Name.LocalName.ToLower().Equals(prop.Name.ToLower()));

                    if(attribute != null)
                    {
                        prop.SetValue(obj, Convert.ChangeType(attribute.Value, prop.PropertyType));
                        attribute.Remove();
                    }
                }
            }
        }
        return obj;
    }

    private IList DeserializeList(Type type)
    {
        var obj = Activator.CreateInstance(type);
        IList list = (IList)obj;

        string el = type.GetGenericArguments()[0].ToString().ToLower();

        if (el.Contains(Assembly.GetCallingAssembly().GetName().Name.ToLower()))
            el = el.Split('.').Last();

        var parent = _xdoc.Descendants().Where(e => e.HasAttributes && e.Name.LocalName.ToLower() == el).FirstOrDefault().Parent;

        if (parent == null)
            throw new Exception("Error while make parent for element in deserialize list...");

        foreach(var item in parent.Descendants().Where(e => e.Name.LocalName.ToLower() == el))
        {
            foreach(var prop in type.GetProperties())
            {
                var propType = prop.PropertyType;
                if(!propType.IsPrimitive && !propType.IsSealed)
                {
                    var child = Deserialize(propType);
                    list.Add(child);
                }
            }
        }
        return list;
    }

    private Type GetImplement(Type type)
    {
        string el = type.Name.ToLower();
        string className = _xdoc.Descendants().FirstOrDefault(e
            => e.HasAttributes && e.Name.LocalName.ToLower() == el)
            .FirstAttribute.Value;

        var typeInfo = Assembly.GetCallingAssembly().GetTypes().FirstOrDefault(s => s.FullName.Contains(className));

        return typeInfo;
    }
}