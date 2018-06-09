﻿using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;
using DefaultEcs.System;

namespace DefaultEcs.Benchmark.DefaultEcs
{
    public static class EntitySetExtension
    {
        public delegate void EntitySetProcess(ReadOnlySpan<Entity> entities);

        public static void ProcessInParallel(this EntitySet set, EntitySetProcess action)
        {
            int entitiesPerCpu = set.Count / Environment.ProcessorCount;

            Enumerable.Range(0, Environment.ProcessorCount).AsParallel().ForAll(i =>
            {
                action(i + 1 == Environment.ProcessorCount ? set.GetEntities().Slice(i * entitiesPerCpu) : set.GetEntities().Slice(i * entitiesPerCpu, entitiesPerCpu));
            });
        }
    }

    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 10, targetCount: 10, invocationCount: 10)]
    public class System
    {
        private struct Position
        {
            public float X;
            public float Y;
        }

        private struct Speed
        {
            public float X;
            public float Y;
        }

        private sealed class TestSystem : AEntitySetSystem<float>
        {
            public TestSystem(World world, SystemRunner<float> runner)
                : base(world.GetEntities().With<Position>().With<Speed>().Build(), runner)
            { }

            protected override void Update(float state, ReadOnlySpan<Entity> entities)
            {
                foreach (Entity entity in entities)
                {
                    ref Position position = ref entity.Get<Position>();

                    position.X += entity.Get<Speed>().X * state;
                    position.Y += entity.Get<Speed>().Y * state;
                }
            }
        }

        private sealed class TestSystem2 : AEntitySetSystem<float>
        {
            public TestSystem2(World world, SystemRunner<float> runner)
                : base(world.GetEntities().With<Position>().With<Speed>().Build(), runner)
            { }

            protected override void Update(float state, ReadOnlySpan<Entity> entities)
            {
                foreach (Entity entity in entities)
                {
                    Speed speed = entity.Get<Speed>();
                    ref Position position = ref entity.Get<Position>();

                    position.X += speed.X * state;
                    position.Y += speed.Y * state;
                }
            }
        }

        private sealed class TestSystemTPL : ISystem<float>
        {
            private readonly EntitySet _set;

            public TestSystemTPL(World world)
            {
                _set = world.GetEntities().With<Position>().With<Speed>().Build();
            }

            public void Update(float state)
            {
                _set.ProcessInParallel(entities =>
                {
                    foreach (Entity entity in entities)
                    {
                        Speed speed = entity.Get<Speed>();
                        ref Position position = ref entity.Get<Position>();

                        position.X += speed.X * state;
                        position.Y += speed.Y * state;
                    }
                });
            }
        }

        private World _world;
        private SystemRunner<float> _runner;
        private ISystem<float> _systemSingle;
        private ISystem<float> _system;
        private ISystem<float> _system2;
        private ISystem<float> _systemTPL;

        [Params(1000000)]
        public int EntityCount { get; set; }

        [IterationSetup]
        public void Setup()
        {
            _world = new World(EntityCount);
            _runner = new SystemRunner<float>(Environment.ProcessorCount);
            _systemSingle = new TestSystem(_world, null);
            _system = new TestSystem(_world, _runner);
            _system2 = new TestSystem2(_world, _runner);
            _systemTPL = new TestSystemTPL(_world);

            for (int i = 0; i < EntityCount; ++i)
            {
                Entity entity = _world.CreateEntity();
                entity.Set<Position>();
                entity.Set(new Speed { X = 1, Y = 1 });
            }
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _runner.Dispose();
            _world.Dispose();
        }

        [Benchmark]
        public void DefaultEcs_UpdateSingle() => _systemSingle.Update(1f / 60f);

        [Benchmark]
        public void DefaultEcs_Update() => _system.Update(1f / 60f);

        [Benchmark]
        public void DefaultEcs_Update2() => _system2.Update(1f / 60f);

        [Benchmark]
        public void DefaultEcs_UpdateTPL() => _systemTPL.Update(1f / 60f);
    }
}