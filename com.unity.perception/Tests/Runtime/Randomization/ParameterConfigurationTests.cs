using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.ParameterBehaviours;
using UnityEngine.Perception.Randomization.Parameters;

namespace RandomizationTests
{
    [TestFixture]
    public class ParameterConfigurationTests
    {
        GameObject m_TestObject;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        // [Test]
        // public void CheckForParametersWithSameNameTest()
        // {
        //     var config = m_TestObject.AddComponent<ParameterList>();
        //     config.AddParameter<FloatParameter>("SameName");
        //     config.AddParameter<BooleanParameter>("SameName");
        //     Assert.Throws<ParameterListException>(() => config.Validate());
        // }

        [Test]
        public void AddingNonParameterTypesTest()
        {
            var config = m_TestObject.AddComponent<ParameterList>();
            Assert.DoesNotThrow(() => config.AddParameter("TestParam1", typeof(FloatParameter)));
            Assert.Throws<ParameterListException>(() => config.AddParameter("TestParam2", typeof(Rigidbody)));
        }
    }
}
