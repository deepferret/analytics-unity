using NUnit.Framework;
using UnityEngine;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Transport")]
    public class WebGLTransportTests
    {
        // --- WebGLBridge Stubs ---

        [Test]
        public void WebGLBridge_SendBatch_StubDoesNotThrowOnNonWebGL()
        {
            Assert.That(() =>
            {
                WebGLBridge.DataFerret_SendBatch(
                    "https://collect.dataferret.io/v1/collect/batch",
                    "{\"batch\":[],\"sentAt\":\"2026-01-01T00:00:00Z\"}",
                    "Bearer wk_test123",
                    "TestObject",
                    "OnComplete");
            }, Throws.Nothing);
        }

        [Test]
        public void WebGLBridge_SendBeacon_StubDoesNotThrowOnNonWebGL()
        {
            Assert.That(() =>
            {
                WebGLBridge.DataFerret_SendBeacon(
                    "https://collect.dataferret.io/v1/collect/batch",
                    "{\"batch\":[],\"sentAt\":\"2026-01-01T00:00:00Z\"}");
            }, Throws.Nothing);
        }

        // --- WebGLTransport.OnSendBatchComplete ---

        [Test]
        public void OnSendBatchComplete_WithSuccessResult_SetsSuccessTrue()
        {
            var go = new GameObject("WebGLTransportTest_Success");
            try
            {
                var transport = go.AddComponent<WebGLTransport>();
                transport.Initialize("https://collect.dataferret.io", "wk_test");

                transport.OnSendBatchComplete("1");

                // Verify via a second SendBatch that the internal state was set.
                // We use reflection-free verification: start a coroutine-like check
                // by calling OnSendBatchComplete and then verifying the result
                // through the callback mechanism.
                bool? callbackResult = null;
                var enumerator = transport.SendBatch(
                    new System.Collections.Generic.List<EventEnvelope>(),
                    success => callbackResult = success);

                // OnSendBatchComplete was already called before SendBatch,
                // so _completed is true from the prior call. Reset by calling
                // SendBatch which resets _completed and _success, then call
                // OnSendBatchComplete again.
                transport.OnSendBatchComplete("1");

                // Advance the coroutine once — it should see _completed = true
                // and exit the loop immediately.
                enumerator.MoveNext();

                Assert.That(callbackResult, Is.True,
                    "OnSendBatchComplete('1') should signal success");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnSendBatchComplete_WithFailureResult_SetsSuccessFalse()
        {
            var go = new GameObject("WebGLTransportTest_Failure");
            try
            {
                var transport = go.AddComponent<WebGLTransport>();
                transport.Initialize("https://collect.dataferret.io", "wk_test");

                bool? callbackResult = null;
                var enumerator = transport.SendBatch(
                    new System.Collections.Generic.List<EventEnvelope>(),
                    success => callbackResult = success);

                // Simulate JavaScript returning failure
                transport.OnSendBatchComplete("0");

                // Advance the coroutine — it should see _completed = true
                enumerator.MoveNext();

                Assert.That(callbackResult, Is.False,
                    "OnSendBatchComplete('0') should signal failure");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
