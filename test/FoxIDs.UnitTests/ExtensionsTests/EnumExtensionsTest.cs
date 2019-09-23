//using Newtonsoft.Json;
//using Xunit;

//namespace FoxIDs.UnitTests.ExtensionsTests
//{
//    public class EnumExtensionsTest
//    {
//        public enum TestTypes
//        {
//            [JsonProperty(PropertyName = "some_test")]
//            SomeTest,
//            AnotherTest
//        }

//        [Theory]
//        [InlineData(TestTypes.SomeTest, "some_test")]
//        [InlineData(TestTypes.AnotherTest, "AnotherTest")]
//        public void ConvertEnum_Equal(TestTypes testTypes, string value)
//        {
//            var valueResult = testTypes.ToValue();
//            Assert.Equal(value, valueResult);

//            var enumResult = value.ToEnum<TestTypes>();
//            Assert.Equal(testTypes, enumResult);
//        }

//        [Theory]
//        [InlineData(TestTypes.SomeTest, "SomeTest")]
//        [InlineData(TestTypes.AnotherTest, "another_test")]
//        public void ConvertEnum_NotEqual(TestTypes testTypes, string value)
//        {
//            var valueResult = testTypes.ToValue();
//            Assert.NotEqual(value, valueResult);

//            var enumResult = value.ToEnum<TestTypes>();
//            Assert.NotEqual(testTypes, enumResult);
//        }
//    }
//}
