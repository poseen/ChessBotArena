﻿using BoardGame.Algorithms.Greedy;
using BoardGame.Algorithms.Tests.Unit.TestCaseClasses;
using Xunit;

namespace BoardGame.Algorithms.Tests.Unit
{
    public class GreedyTests
    {
        [Fact]
        public void Test1()
        {
            var evaluator = new TestCase1.Evaluator();
            var generator = new TestCase1.Generator();
            var applier = new TestCase1.Applier();
            var algorithm = new GreedyAlgorithm<TestCase1.State, TestCase1.Move>(evaluator, generator, applier);

            var initState = new TestCase1.State(1, int.MinValue);

            var move1 = algorithm.Calculate(initState);
            var state2 = applier.Apply(initState, move1);
            var move2 = algorithm.Calculate(state2);

            Assert.Equal('a', move1.Label);
            Assert.Equal('c', move2.Label);
        }
    }
}
