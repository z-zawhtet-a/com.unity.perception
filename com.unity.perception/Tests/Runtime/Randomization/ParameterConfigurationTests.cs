using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.TestTools;

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

        [Test]
        public void CheckForParametersWithSameNameTest()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            config.AddParameter<FloatParameter>("SameName");
            config.AddParameter<BooleanParameter>("SameName");
            Assert.Throws<ParameterConfigurationException>(() => config.ValidateParameters());
        }

        [Test]
        public void AddingNonParameterTypesTest()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            Assert.DoesNotThrow(() => config.AddParameter("TestParam1", typeof(FloatParameter)));
            Assert.Throws<ParameterConfigurationException>(() => config.AddParameter("TestParam2", typeof(Rigidbody)));
        }
    }
}
