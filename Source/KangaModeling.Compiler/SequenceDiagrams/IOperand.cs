﻿using System.Collections.Generic;

namespace KangaModeling.Compiler.SequenceDiagrams
{
    public interface IOperand
    {
        ICombinedFragment Parent { get; }
        string GuardExpression { get; }
        IEnumerable<IActivity> Activities { get; }
        IEnumerable<ISignal> Signals { get; }
        IEnumerable<ICombinedFragment> Children { get; }
    }
}