﻿using System;
using DefaultEcs.System;
using NFluent;
using Xunit;

namespace DefaultEcs.Test.System
{
    public sealed class ActionSystemTest
    {
        #region Tests

        [Fact]
        public void Update_Should_throw_ArgumentNullException_When_action_is_null() => Check
            .ThatCode(() => new ActionSystem<int>(null))
            .Throws<ArgumentNullException>()
            .WithProperty(e => e.ParamName, "action");

        [Fact]
        public void Update_Should_call_the_action()
        {
            bool done = false;

            using ISystem<int> system = new ActionSystem<int>(_ => done = true);

            system.Update(0);

            Check.That(done).IsTrue();
        }

        [Fact]
        public void Update_Should_not_call_the_action_When_disabled()
        {
            bool done = false;

            using ISystem<int> system = new ActionSystem<int>(_ => done = true)
            {
                IsEnabled = false
            };

            system.Update(0);

            Check.That(done).IsFalse();
        }

        #endregion
    }
}
