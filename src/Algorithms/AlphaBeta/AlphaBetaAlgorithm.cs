﻿using System;
using System.Linq;
using Algorithms.Abstractions.Interfaces;
using Algorithms.Minimax.Exceptions;

namespace Algorithms.AlphaBeta
{
    /// <summary>
    /// The alpha-beta algorithm implementation.
    /// </summary>
    /// <typeparam name="TState">The type of the states which have to be evaluated.</typeparam>
    /// <typeparam name="TMove">The type of the moves between states.</typeparam>
    public class AlphaBetaAlgorithm<TState, TMove> : IAlgorithm<TState, TMove>
        where TMove : class
    {
        private int _maxDepth;

        protected readonly IEvaluator<TState> _evaluator;
        protected readonly ITransitionGenerator<TState, TMove> _moveGenerator;
        protected readonly IApplier<TState, TMove> _moveApplier;

        public AlphaBetaAlgorithm(IEvaluator<TState> evaluator, ITransitionGenerator<TState, TMove> moveGenerator, IApplier<TState, TMove> applier)
        {
            _evaluator = evaluator;
            _moveGenerator = moveGenerator;
            _moveApplier = applier;
            _maxDepth = 3;
        }

        /// <summary>
        /// Gets or sets the maximum depth (number of transitions) the algorithm is checking.
        /// </summary>
        public int MaxDepth
        {
            get
            {
                return _maxDepth;
            }

            set
            {
                if (value < 1)
                {
                    throw new MinimaxMaxDepthInvalidException();
                }

                _maxDepth = value;
            }
        }

        /// <inheritdoc />
        public TMove Calculate(TState state, bool maximize = true)
        {
            var moves = _moveGenerator.Generate(state);

            if (!moves.Any())
            {
                return null;
            }

            var movesAndValues = moves.Select(move => new
            {
                Move = move,
                Value = Calculate(_moveApplier.Apply(state, move), 1, int.MaxValue, int.MinValue, !maximize)
            });

            var max = movesAndValues.Max(x => x.Value);
            var result = movesAndValues.First(x => Equals(x.Value, max)).Move;

            return result;
        }

        // https://en.wikipedia.org/wiki/Alpha%E2%80%93beta_pruning
        private int Calculate(TState state, int depth, int alpha, int beta, bool maximize)
        {
            if (depth == MaxDepth)
            {
                return _evaluator.Evaluate(state);
            }

            var nextMoves = _moveGenerator.Generate(state).ToList();
            var isLeaf = !nextMoves.Any();

            if (isLeaf)
            {
                return _evaluator.Evaluate(state);
            }

            if (maximize)
            {
                var maximizerValue = int.MinValue;

                foreach (var mv in nextMoves)
                {
                    maximizerValue = (int)Math.Max(maximizerValue, Calculate(_moveApplier.Apply(state, mv), depth + 1, alpha, beta, false));
                    alpha = (int)Math.Max(alpha, maximizerValue);

                    if(alpha >= beta)
                    {
                        break;
                    }
                }

                return maximizerValue;
            }
            else
            {
                var minimizerValue = int.MaxValue;

                foreach (var mv in nextMoves)
                {
                    minimizerValue = (int)Math.Min(minimizerValue, Calculate(_moveApplier.Apply(state, mv), depth + 1, alpha, beta, true));
                    beta = (int)Math.Min(beta, minimizerValue);

                    if (alpha >= beta)
                    {
                        break;
                    }
                }

                return minimizerValue;
            }
        }
    }
}
