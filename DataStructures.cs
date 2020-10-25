using System;
using System.Text;

namespace RZ.App
{
    static class StringExtension
    {
        public static string Left(this string s, int length) {
            var l = Math.Min(length, s.Length);
            return s[..l];
        }
    }

    public record Person(int Id, string Name, int Age);

    public sealed class PersonBinarySerializer : IBinarySerializer<Person>
    {
        const int NameLength = 20;
        const int BinarySize = sizeof(int) /* id */ + 40 /* CHAR(20) */ + sizeof(int) /* age */;

        public int BlockSize => BinarySize;

        public byte[] Serialize(Person data) {
            var block = new byte[BinarySize];
            var idBlock = BitConverter.GetBytes(data.Id);
            var nameBlock = Encoding.UTF8.GetBytes(data.Name.Left(NameLength));
            var ageBlock = BitConverter.GetBytes(data.Age);
            Array.Copy(idBlock, block, idBlock.Length);
            Array.Copy(nameBlock, 0, block, idBlock.Length, Math.Min(NameLength, nameBlock.Length));
            Array.Copy(ageBlock, 0, block, idBlock.Length + NameLength, ageBlock.Length);
            return block;
        }

        public Person Deserialize(byte[] binary) {
            var id = BitConverter.ToInt32(binary);
            var name = Encoding.UTF8.GetString(binary, sizeof(int), NameLength);
            var age = BitConverter.ToInt32(binary, sizeof(int) + NameLength);
            return new (id, name, age);
        }
    }

}