﻿using System;

namespace KangaModeling.Visuals.SequenceDiagrams
{
    internal class Column
    {
        public const int MinWidth = 40;

        private float m_MinWidth;

        public Column()
        {
            m_MinWidth = MinWidth + 10;
            LeftGap = new ColumnSection();
            Body = new ColumnSection();
            RightGap = new ColumnSection();
            Body.Allocate(MinWidth);
        }

        public ColumnSection LeftGap { get; private set; }
        public ColumnSection Body { get; private set; }
        public ColumnSection RightGap { get; private set; }

        public float Width
        {
            get { return LeftGap.Width + Body.Width + RightGap.Width; }
        }

        public float Left
        {
            get { return LeftGap.Left; }
        }

        public float Right
        {
            get { return RightGap.Right; }
        }

        public void Allocate(float width)
        {
            m_MinWidth = Math.Max(m_MinWidth, width);
        }

        public void AdjustLocation(float x)
        {
            ExtendGaps();
            AdjustSectionLocations(x);
        }

        private void AdjustSectionLocations(float x)
        {
            LeftGap.Left = x;
            Body.Left = LeftGap.Right;
            RightGap.Left = Body.Right;
        }

        private void ExtendGaps()
        {
            if (m_MinWidth > Width)
            {
                float delta = (m_MinWidth - Body.Width)/2;
                LeftGap.Allocate(delta);
                RightGap.Allocate(delta);
            }
        }
    }
}