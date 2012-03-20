﻿using KangaModeling.Compiler.SequenceDiagrams.Model;
using KangaModeling.Compiler.SequenceDiagrams.Reading;

namespace KangaModeling.Compiler.SequenceDiagrams.Ast
{
    internal class AstBuilder
    {
        private readonly SequenceDiagram m_Diagram;

        public SequenceDiagram Diagram
        {
            get { return m_Diagram; }
        }

        public AstBuilder(SequenceDiagram diagram)
        {
            m_Diagram = diagram;
        }

        public void SetTitle(string title)
        {
            m_Diagram.Title = title;
        }

        public void AddError(Token invalidToken, string text)
        {

        }
    }
}