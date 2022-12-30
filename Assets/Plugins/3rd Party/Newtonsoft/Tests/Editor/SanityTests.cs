using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Unity.Nuget.NewtonsoftJson.Tests
{
    public class SanityTests
    {
        [Test]
        public void TestWithString()
        {
            Assert.IsTrue(true);
            const string testString = "test-string";
            var json = JsonConvert.SerializeObject(testString);
            var newString = JsonConvert.DeserializeObject<string>(json);
            Assert.AreEqual(testString, newString);
        }

        [Test]
        public void TestWithCustomObject()
        {
            var testObject = new TestObject
            {
                TestInt = 3, TestString = "test-string", TestList = new List<string> { "test1", "test2" }
            };
            var json = JsonConvert.SerializeObject(testObject);
            var newObject = JsonConvert.DeserializeObject<TestObject>(json);
            Assert.AreEqual(testObject.TestInt, newObject.TestInt);
            Assert.AreEqual(testObject.TestString, newObject.TestString);
            for (var i = 0; i < 2; i++)
            {
                Assert.AreEqual(testObject.TestList[i], newObject.TestList[i]);
            }
        }

        class TestObject
        {
            public string TestString;
            public List<string> TestList;
            public int TestInt;
        }
    }
}