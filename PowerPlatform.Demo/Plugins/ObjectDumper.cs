using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public class ObjectDumper
{
    private int _level;
    private readonly int _indentSize;
    private readonly StringBuilder _stringBuilder;
    private readonly List<int> _hashListOfFoundElements;

    private ObjectDumper(int indentSize)
    {
        this._indentSize = indentSize;
        this._stringBuilder = new StringBuilder();
        this._hashListOfFoundElements = new List<int>();
    }

    public static string Dump(object element)
    {
        return Dump(element, 2);
    }

    public static string Dump(object element, int indentSize)
    {
        var instance = new ObjectDumper(indentSize);
        return instance.DumpElement(element);
    }

    private string DumpElement(object element)
    {
        if (element == null || element is ValueType || element is string)
        {
            Write(FormatValue(element));
        }
        else
        {
            var objectType = element.GetType();
            if (!typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                Write("{{{0}}}", objectType.FullName);
                this._hashListOfFoundElements.Add(element.GetHashCode());
                this._level++;
            }

            if (element is IEnumerable enumerableElement)
            {
                foreach (object item in enumerableElement)
                {
                    if (item is IEnumerable && !(item is string))
                    {
                        this._level++;
                        DumpElement(item);
                        this._level--;
                    }
                    else
                    {
                        if (!AlreadyTouched(item))
                            DumpElement(item);
                        else
                            Write("{{{0}}} <-- bidirectional reference found", item.GetType().FullName);
                    }
                }
            }
            else
            {
                MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                foreach (var memberInfo in members)
                {
                    var fieldInfo = memberInfo as FieldInfo;
                    var propertyInfo = memberInfo as PropertyInfo;

                    if (fieldInfo == null && propertyInfo == null)
                        continue;

                    var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                    object value;

                    try
                    {
                        value = fieldInfo != null
                                           ? fieldInfo.GetValue(element)
                                           : propertyInfo.GetValue(element, null);
                    }
                    catch (Exception ex)
                    {
                        value = ex.Message;
                    }

                    if (type.IsValueType || type == typeof(string))
                    {
                        Write("{0}: {1}", memberInfo.Name, FormatValue(value));
                    }
                    else
                    {
                        var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                        Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                        var alreadyTouched = !isEnumerable && AlreadyTouched(value);
                        this._level++;
                        if (!alreadyTouched)
                            DumpElement(value);
                        else
                            Write("{{{0}}} <-- bidirectional reference found", value.GetType().FullName);
                        this._level--;
                    }
                }
            }

            if (!typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                this._level--;
            }
        }

        return this._stringBuilder.ToString();
    }

    private bool AlreadyTouched(object value)
    {
        if (value == null)
            return false;

        var hash = value.GetHashCode();
        for (var i = 0; i < this._hashListOfFoundElements.Count; i++)
        {
            if (this._hashListOfFoundElements[i] == hash)
                return true;
        }
        return false;
    }

    private void Write(string value, params object[] args)
    {
        var space = new string(' ', this._level * this._indentSize);

        if (args != null)
            value = string.Format(value, args);

        this._stringBuilder.AppendLine(space + value);
    }

    private string FormatValue(object o)
    {
        if (o == null)
            return "null";

        if (o is DateTime time)
            return time.ToShortDateString();

        if (o is string)
            return string.Format("\"{0}\"", o);

        if (o is char v && v == '\0')
            return string.Empty;

        if (o is ValueType)
            return o.ToString();

        if (o is IEnumerable)
            return "...";

        return "{ }";
    }
}