using MongoDB.Bson;

namespace Logistics.API.Extensions
{
    public static class ToBsonDocumentArray
    {
        public static BsonArray ToBsonDocumentArrayExt(Array list)
        {
            var array = new BsonArray();
            foreach (var item in list)
            {
                array.Add(item.ToBson());
            }
            return array;
        }
    }
}
