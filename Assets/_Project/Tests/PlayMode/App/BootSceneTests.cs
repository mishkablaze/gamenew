using System.Collections;
using NUnit.Framework;
using Raidline.App.Boot;
using Raidline.Infrastructure.Ticking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Raidline.Tests.PlayMode.App
{
    /// <summary>
    /// Loads the real Boot scene: Fish-Net host must start and drive TickRunner at ~60 ticks/s,
    /// pre-physics and post-physics stages of the same tick must run in order.
    /// </summary>
    public sealed class BootSceneTests
    {
        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

        private sealed class StageProbe : ITickable
        {
            private readonly bool _isPre;
            private readonly StageProbe _pre;

            public uint LastTick;
            public int OrderErrors;

            public StageProbe(bool isPre, StageProbe pre)
            {
                _isPre = isPre;
                _pre = pre;
            }

            public void Tick(in TickContext ctx)
            {
                if (!_isPre && _pre.LastTick != ctx.Tick)
                {
                    OrderErrors++;
                }

                LastTick = ctx.Tick;
            }
        }

        private AppRoot _root;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Boot_StartsHostAndTicksAtSixtyHz()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(BootScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor-only test.");
#endif
            yield return null;

            _root = Object.FindAnyObjectByType<AppRoot>();
            Assert.That(_root, Is.Not.Null, "AppRoot not found in Boot scene");
            Assert.That(_root.Context, Is.Not.Null, "AppRoot did not initialize");

            float waited = 0f;
            while (!_root.Context.Session.IsHostReady && waited < 10f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(_root.Context.Session.IsHostReady, Is.True, "host did not start within 10 s");

            var pre = new StageProbe(true, null);
            var post = new StageProbe(false, pre);
            _root.Context.TickRunner.Register(TickPhase.Characters, pre);
            _root.Context.TickRunner.Register(TickPhase.Raid, post);

            ulong startTicks = _root.Context.TickRunner.CompletedTicks;
            double startTime = Time.realtimeSinceStartupAsDouble;
            yield return new WaitForSecondsRealtime(3f);
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            double rate = (_root.Context.TickRunner.CompletedTicks - startTicks) / elapsed;

            Assert.That(rate, Is.InRange(55d, 65d), "ticks per second");
            Assert.That(post.OrderErrors, Is.EqualTo(0), "post-physics stage ran without matching pre-physics stage");
            Assert.That(Physics.simulationMode, Is.EqualTo(SimulationMode.Script), "physics must be stepped by TimeManager");
        }
    }
}
