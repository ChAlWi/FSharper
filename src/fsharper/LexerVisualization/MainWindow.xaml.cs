﻿using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace JetBrains.ReSharper.FSharp.LexerVisualization
{
  using System;
  using System.Net;
  using System.Text;
  using System.Windows;
  using Psi.FSharp.Parsing;
  using Psi.Parsing;
  using Text;

  public partial class MainWindow : Window
  {
    public static readonly DependencyProperty InputTextProperty = 
      DependencyProperty.Register("InputText", typeof(string), typeof(MainWindow), 
      new PropertyMetadata(string.Empty, InputTextChanged));

    private static void InputTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ((MainWindow)d).Process();
    }

    public MainWindow()
    {
      InitializeComponent();
    }

    public string InputText
    {
      get { return (string)GetValue(InputTextProperty); }
      set { SetValue(InputTextProperty, value); }
    }

    class Implementation
    {
      public static string Start(string inputText)
      {
        if (inputText == null) throw new ArgumentNullException("inputText");
        return Lex(inputText);
      }

      private static string Lex(string inputText)
      {
        var sb = new StringBuilder();
        var lexer = new FSharpLexer(new StringBuffer(inputText));
        int position = 0;
        lexer.Start();
        try
        {
          while (lexer.TokenType != null)
          {
            if (lexer.TokenStart != position) sb.AppendFormat("<pre>Token start error. Expected: {0}, actual: {1}</pre>", position, lexer.TokenStart);

            //Assert.AreEqual(lexer.TokenStart, position, "Token start error. Expected: {0}, actual: {1}", position, lexer.TokenStart);
            position = lexer.TokenEnd;

            // 
            var token = lexer.TokenType;
            var len = lexer.TokenEnd - lexer.TokenStart;
            char[] buffer = new char[len];
            lexer.Buffer.CopyTo(lexer.TokenStart, buffer, 0, len);
            string tokenText = WebUtility.HtmlEncode(new string(buffer));
            if (token.IsComment) RenderComment(sb, tokenText, token);
            else if (token.IsKeyword) RenderKeyword(sb, tokenText, token);
            else if (token.IsWhitespace) RenderWhitespace(sb, tokenText, token);
            else if (token.IsConstantLiteral) RenderConstantLiteral(sb, tokenText, token);
            else if (token.IsStringLiteral) RenderStringLiteral(sb, tokenText, token);
            else if (token.IsIdentifier) RenderIdentifier(sb, tokenText, token);
            else if (token == FSharpTokenType.BAD_CHARACTER)
            {
              RenderBad(sb, tokenText, token);
            }
            else
            {
              // something else?
              RenderSomethingElse(sb, tokenText, token);
            }

            lexer.Advance();
          }
        }
        catch (Exception ex)
        {
          sb.AppendFormat("<span style=\"color:red\">{0}</span>", ex);
        }

        //Assert.AreEqual(lexer.Buffer.Length, position, "position == lexer.Buffer.Length");
        return sb.ToString();
      }

      private static void RenderBad(StringBuilder sb, string tokenText, TokenNodeType token)
      {
        sb.AppendFormat("<span title=\"bad character\" style=\"color:red\">{0}</span>", tokenText);
      }

      private static void RenderSomethingElse(StringBuilder sb, string tokenText, TokenNodeType token)
      {
        sb.AppendFormat("<span title=\"maybe operator {1}\">{0}</span>", tokenText, WebUtility.HtmlEncode(tokenText));
      }

      private static void RenderIdentifier(StringBuilder sb, string tokenText, TokenNodeType token)
      {
        sb.AppendFormat("<span title=\"identifier\" style=\"background-color: lightgreen\">{0}</span>", tokenText);
      }

      private static void RenderStringLiteral(StringBuilder sb, string tokenText, TokenNodeType token)
      {
        sb.AppendFormat("<span title=\"comment\" style=\"background-color: #ffe0e0\">{0}</span>", tokenText);
      }

      private static void RenderConstantLiteral(StringBuilder sb, string tokenText, TokenNodeType token)
      {
        sb.AppendFormat("<span title=\"constant literal {0}\" style=\"background-color: #e0e0ff\">{0}</span>", tokenText);
      }

      private static void RenderWhitespace(StringBuilder sb, string tokenText, TokenNodeType token)
      {
        if (Environment.NewLine.Contains(tokenText))
          sb.Append("<br/>");
        else
        {
          sb.AppendFormat("<span title=\"whitespace\" style=\"background-color: #efefef\">");
          for (int i = 0; i < tokenText.Length; ++i)
            sb.Append("&hellip;");
          sb.Append("</span>");
        }
      }

      private static void RenderKeyword(StringBuilder sb, string tokenText, TokenNodeType token)
      {
        sb.AppendFormat("<span title=\"keyword\" style=\"background-color: #e0e0ff\">{0}</span>", tokenText);
      }

      private static void RenderComment(StringBuilder sb, string tokenText, TokenNodeType token)
      {
        sb.AppendFormat("<span title=\"comment\" style=\"background-color: #e0ffe0\">{0}</span>", tokenText);
      }
    }

    private void OnProcess(object sender, RoutedEventArgs e)
    {
      Process();
    }

    private void Process()
    {
      string html = Implementation.Start(InputText);

      const string preamble = "<pre>";
      const string postamble = "<pre>";

      theBrowser.NavigateToString(preamble + html + postamble);
    }

    private void OnOpen(object sender, RoutedEventArgs e)
    {
      var ofd = new OpenFileDialog {Filter = "F# Files|*.fs"};
      if (ofd.ShowDialog() ?? false)
      {
        tbIn.Text = File.ReadAllText(ofd.FileName);
      }
    }
  }
}
