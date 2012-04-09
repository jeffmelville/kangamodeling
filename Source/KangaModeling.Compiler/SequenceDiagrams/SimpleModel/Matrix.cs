﻿using System;
using System.Collections.Generic;

namespace KangaModeling.Compiler.SequenceDiagrams.SimpleModel
{
    internal class Matrix : ISequenceDiagram
    {
        private readonly RootFragment m_Root;

        public Matrix()
        {
            Lifelines = new LifelineCollection(StringComparer.InvariantCultureIgnoreCase);
            Rows = new RowsCollection();
            m_Root = new RootFragment(this);
        }

        public RootFragment Root
        {
            get { return m_Root; }
        }

        public LifelineCollection Lifelines { get; private set; }

        public RowsCollection Rows { get; private set; }

        public Row LastRow
        {
            get { return Rows[Rows.Count - 1]; }
        }

        public Pin this[int lifelineIndex, int rowIndex]
        {
            get { return Rows[rowIndex][lifelineIndex]; }
        }

        #region ISequenceDiagram Members

        IFragment ISequenceDiagram.Root
        {
            get { return m_Root; }
        }

        IEnumerable<ILifeline> ISequenceDiagram.Lifelines
        {
            get { return Lifelines; }
        }

        #endregion

        public Lifeline CreateLifeline(string id, string name)
        {
            var lifeline = new Lifeline(this, id, name, Lifelines.Count);
            Rows.Extend(lifeline);
            Lifelines.Add(lifeline);
            return lifeline;
        }

        public Row CreateRow()
        {
            var row = new Row(Rows.Count);
            row.Extend(Lifelines);
            Rows.Add(row);
            return row;
        }
    }
}