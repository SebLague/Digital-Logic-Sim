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
			string json = JsonConvert.SerializeObject(testString);
			string newString = JsonConvert.DeserializeObject<string>(json);
			Assert.AreEqual(testString, newString);
		}

		[Test]
		public void TestWithCustomObject()
		{
			TestObject testObject = new TestObject
			{
				TestInt = 3, TestString = "test-string", TestList = new List<string> { "test1", "test2" }
			};
			string json = JsonConvert.SerializeObject(testObject);
			TestObject newObject = JsonConvert.DeserializeObject<TestObject>(json);
			Assert.AreEqual(testObject.TestInt, newObject.TestInt);
			Assert.AreEqual(testObject.TestString, newObject.TestString);
			for (int i = 0; i < 2; i++)
			{
				Assert.AreEqual(testObject.TestList[i], newObject.TestList[i]);
			}
		}

		class TestObject
		{
			public int TestInt;
			public List<string> TestList;
			public string TestString;
		}
	}
}