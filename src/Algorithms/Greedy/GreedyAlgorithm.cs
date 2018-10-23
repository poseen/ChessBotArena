﻿using System.Linq;
using Algorithms.Abstractions.Interfaces;

namespace Algorithms.GreedyAlgorithm
{
    /// <summary>
    /// The greedy-algorithm. Tries to ruin your day by always choosing that move which minimizes your next state.
    /// A real jerk.
    /// </summary>
    /// <typeparam name="TState">Type of the states.</typeparam>
    /// <typeparam name="TMove">Type of the moves.</typeparam>
    public class GreedyAlgorithm<TState, TMove> : IAlgorithm<TState, TMove>
        where TMove : class
    {
        private int _maxDepth;

        protected readonly IEvaluator<TState> _evaluator;
        protected readonly IGenerator<TState, TMove> _moveGenerator;
        protected readonly IApplier<TState, TMove> _moveApplier;

        public GreedyAlgorithm(IEvaluator<TState> evaluator, IGenerator<TState, TMove> moveGenerator, IApplier<TState, TMove> applier)
        {
            _evaluator = evaluator;
            _moveGenerator = moveGenerator;
            _moveApplier = applier;
        }

        /// <inheritdoc />
        public TMove Calculate(TState state)
        {
            var moves = _moveGenerator.Generate(state);

            if (!moves.Any())
            {
                return null;
            }

            var movesAndValues = moves.Select(move => new
            {
                Move = move,
                Value = _evaluator.Evaluate(_moveApplier.Apply(state, move))
            });

            var min = movesAndValues.Min(x => x.Value);

            var result = movesAndValues.First(x => Equals(x.Value, min)).Move;

            return result;
        }
    }
}
