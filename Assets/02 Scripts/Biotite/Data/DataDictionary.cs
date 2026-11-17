using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 딕셔너리를 래핑하여 내부 구조를 숨기고 안전하게 데이터를 관리합니다.
/// [JsonConverter]를 통해 JsonConvert와 직접 호환됩니다.
/// </summary>
[JsonConverter(typeof(DataDictionaryConverter))]
public class DataDictionary<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>
{
    private readonly Dictionary<T1, T2> dataDict;
    public IReadOnlyDictionary<T1, T2> Map => dataDict;


    public DataDictionary()
    {
        dataDict = new Dictionary<T1, T2>();
    }


    public DataDictionary(IDictionary<T1, T2> dictionary)
    {
        dataDict = dictionary != null
            ? new Dictionary<T1, T2>(dictionary)
            : new Dictionary<T1, T2>();
    }
    


    /// <summary>
    /// JSON 문자열을 DataDictionary 객체로 역직렬화합니다.
    /// </summary>
    public static DataDictionary<T1, T2> DeserializeObject(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new DataDictionary<T1, T2>();

        return JsonConvert.DeserializeObject<DataDictionary<T1, T2>>(json);
    }

    /// <summary>
    /// 데이터를 JSON 문자열로 직렬화합니다.
    /// </summary>
    public string SerializeObject()
    {
        return JsonConvert.SerializeObject(this);
    }


    /// <summary>
    /// 새로운 키-값 쌍을 추가합니다. (중복 키 방지)
    /// </summary>
    public bool Add(T1 id, T2 data)
    {
        if (id == null) return false;

        if (dataDict.TryAdd(id, data))
        {
            return true;
        }

        Logger.LogError($"[DataDictionary] 중복된 키입니다: {id}");
        return false;
    }

    /// <summary>
    /// 기존 키의 데이터를 업데이트합니다. (키가 존재하는 경우에만 성공)
    /// </summary>
    public bool UpdateData(T1 id, T2 data)
    {
        if (id != null && dataDict.ContainsKey(id))
        {
            dataDict[id] = data;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 키가 존재하면 업데이트하고, 없으면 추가합니다.
    /// </summary>
    public void SetOrUpdate(T1 id, T2 data)
    {
        if (id == null) return;
        dataDict[id] = data;
    }

    /// <summary>
    /// 특정한 키 데이터를 삭제합니다.
    /// </summary>
    public bool Remove(T1 id)
    {
        if (id == null) return false;
        return dataDict.Remove(id);
    }

    /// <summary>
    /// 현재 해당하는 키 값이 존재하는지 확인합니다.
    /// </summary>
    public bool ContainsKey(T1 id) => id != null && dataDict.ContainsKey(id);

    /// <summary>
    /// id에 대한 값을 가져옵니다.
    /// </summary>
    public bool TryGetValue(T1 id, out T2 value)
    {
        if (id == null)
        {
            value = default;
            return false;
        }

        return dataDict.TryGetValue(id, out value);
    }
    
    
    /// <summary>
    /// 리스트로 변환해서 값을 복사해서 넘겨줍니다.
    /// </summary>
    /// <returns></returns>
    public List<KeyValuePair<T1, T2>> ToList()
    {
        return new List<KeyValuePair<T1, T2>>(dataDict);
    }

    // foreach 지원을 위한 IEnumerable 구현 
    public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator() => dataDict.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}



/// <summary>
/// DataDictionary 전용 Newtonsoft.Json Converter
/// JSON <-> DataDictionary 간 직렬화시 불필요한 임시 객체 생성을 막아줍니다.
/// </summary>
public class DataDictionaryConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(DataDictionary<,>);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var mapProp = value.GetType().GetProperty("Map");
        var mapValue = mapProp?.GetValue(value);
        serializer.Serialize(writer, mapValue);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var genericArgs = objectType.GetGenericArguments();
        var dictType = typeof(Dictionary<,>).MakeGenericType(genericArgs);
        var dict = serializer.Deserialize(reader, dictType);

        return Activator.CreateInstance(objectType, dict);
    }
}