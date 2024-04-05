using FoxIDs.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Newtonsoft.Json;
using System.Reflection;

namespace FoxIDs.Repository
{
    public class MongoDbJsonPropertyConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap memberMap)
        {
            memberMap.SetElementName(GetElementName(memberMap) ?? memberMap.MemberName);
        }

        private string GetElementName(BsonMemberMap memberMap)
        {
            if (memberMap.MemberName != nameof(DataElement.Id))
            {
                var jsonPropertyAtt = memberMap.MemberInfo.GetCustomAttribute<JsonPropertyAttribute>();
                if (!string.IsNullOrEmpty(jsonPropertyAtt?.PropertyName))
                {
                    return jsonPropertyAtt.PropertyName;
                }
            }
            return null;
        }
    }
}
