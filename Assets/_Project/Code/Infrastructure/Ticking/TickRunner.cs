using System;
using Raidline.Core.Common;
using Unity.Profiling;

namespace Raidline.Infrastructure.Ticking
{
    /// <summary>
    /// Runs registered <see cref="ITickable"/> systems phase by phase on every tick of an <see cref="ITickSource"/>
    /// (ARCHITECTURE §5). Pre-physics phases run on <see cref="ITickSource.PrePhysicsTick"/>,
    /// post-physics phases on <see cref="ITickSource.PostPhysicsTick"/>.
    /// Systems may register or unregister from inside a tick: removals take effect immediately,
    /// additions from the next tick (<see cref="SafeIterationList{T}"/>).
    /// Plain C# class: created by the composition root, not a MonoBehaviour.
    /// </summary>
    public sealed class TickRunner
    {
        private static readonly ProfilerMarker[] s_phaseMarkers = CreateMarkers();

        private readonly SafeIterationList<ITickable>[] _phases;
        private ITickSource _source;

        public TickRunner()
        {
            _phases = new SafeIterationList<ITickable>[TickPhases.Count];
            for (int i = 0; i < _phases.Length; i++)
            {
                _phases[i] = new SafeIterationList<ITickable>();
            }
        }

        /// <summary>Number of fully completed ticks (both stages).</summary>
        public ulong CompletedTicks { get; private set; }

        /// <summary>Tick number of the last tick that started.</summary>
        public uint LastTick { get; private set; }

        /// <summary>The source this runner is attached to, or null.</summary>
        public ITickSource Source => _source;

        /// <summary>Adds a system to a phase. Returns false if it is already in that phase.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="tickable"/> is null.</exception>
        public bool Register(TickPhase phase, ITickable tickable)
        {
            if (tickable == null)
            {
                throw new ArgumentNullException(nameof(tickable));
            }

            return _phases[(int)phase].Add(tickable);
        }

        /// <summary>Removes a system from a phase. Returns false if it was not there.</summary>
        public bool Unregister(TickPhase phase, ITickable tickable)
        {
            return _phases[(int)phase].Remove(tickable);
        }

        /// <summary>Number of systems registered in a phase.</summary>
        public int CountIn(TickPhase phase)
        {
            return _phases[(int)phase].Count;
        }

        /// <summary>Subscribes to a tick source (detaching from the previous one, if any).</summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public void Attach(ITickSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Detach();
            _source = source;
            _source.PrePhysicsTick += RunPrePhysics;
            _source.PostPhysicsTick += RunPostPhysics;
        }

        /// <summary>Unsubscribes from the current tick source.</summary>
        public void Detach()
        {
            if (_source == null)
            {
                return;
            }

            _source.PrePhysicsTick -= RunPrePhysics;
            _source.PostPhysicsTick -= RunPostPhysics;
            _source = null;
        }

        /// <summary>Runs the pre-physics phases (Input, Characters, Weapons).</summary>
        public void RunPrePhysics(in TickContext ctx)
        {
            LastTick = ctx.Tick;
            for (int phase = 0; phase < (int)TickPhases.FirstPostPhysics; phase++)
            {
                RunPhase(phase, in ctx);
            }
        }

        /// <summary>Runs the post-physics phases (Ballistics, Damage, AI, Raid).</summary>
        public void RunPostPhysics(in TickContext ctx)
        {
            for (int phase = (int)TickPhases.FirstPostPhysics; phase < TickPhases.Count; phase++)
            {
                RunPhase(phase, in ctx);
            }

            CompletedTicks++;
        }

        private void RunPhase(int phase, in TickContext ctx)
        {
            SafeIterationList<ITickable> list = _phases[phase];
            s_phaseMarkers[phase].Begin();
            int count = list.BeginIteration();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    ITickable tickable = list[i];
                    if (tickable != null)
                    {
                        tickable.Tick(in ctx);
                    }
                }
            }
            finally
            {
                list.EndIteration();
                s_phaseMarkers[phase].End();
            }
        }

        private static ProfilerMarker[] CreateMarkers()
        {
            var markers = new ProfilerMarker[TickPhases.Count];
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = new ProfilerMarker("Raidline.Tick." + (TickPhase)i);
            }

            return markers;
        }
    }
}
