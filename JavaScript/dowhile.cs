// dowhile.cs
//
// Copyright 2010 Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Ajax.Utilities
{

    public sealed class DoWhile : AstNode
    {
        public Block Body { get; set; }
        public AstNode Condition {get; set;}

        public DoWhile(Context context, JSParser parser, AstNode body, AstNode condition)
            : base(context, parser)
        {
            Body = ForceToBlock(body);
            Condition = condition;
            if (Body != null) Body.Parent = this;
            if (Condition != null) Condition.Parent = this;
        }

        public override AstNode Clone()
        {
            return new DoWhile(
              (Context == null ? null : Context.Clone()),
              Parser,
              (Body == null ? null : Body.Clone()),
              (Condition == null ? null : Condition.Clone())
              );
        }

        internal override void AnalyzeNode()
        {
            // if we are stripping debugger statements and the body is
            // just a debugger statement, replace it with a null
            if (Parser.Settings.StripDebugStatements
                 && Parser.Settings.IsModificationAllowed(TreeModifications.StripDebugStatements) 
                 && Body != null 
                 && Body.IsDebuggerStatement)
            {
                Body = null;
            }

            // recurse
            base.AnalyzeNode();

            // if the body is now empty, make it null
            if (Body != null && Body.Count == 0)
            {
                Body = null;
            }
        }

        public override IEnumerable<AstNode> Children
        {
            get
            {
                if (Body != null)
                {
                    yield return Body;
                }
                if (Condition != null)
                {
                    yield return Condition;
                }
            }
        }

        public override bool ReplaceChild(AstNode oldNode, AstNode newNode)
        {
            if (Body == oldNode)
            {
                Body = ForceToBlock(newNode);
                if (Body != null) { Body.Parent = this; }
                return true;
            }
            if (Condition == oldNode)
            {
                Condition = newNode;
                if (newNode != null) { newNode.Parent = this; }
                return true;
            }
            return false;
        }

        internal override bool EncloseBlock(EncloseBlockType type)
        {
            // there is an IE bug (up to IE7, at this time) that do-while
            // statements cause problems when they happen before else or while
            // statements without a closing curly-brace between them.
            // So if we get here, flag this as possibly requiring a block.
            return (type == EncloseBlockType.SingleDoWhile);
        }

        public override string ToCode(ToCodeFormat format)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("do");

            ToCodeFormat bodyFormat = ((Body != null
              && Body.Count == 1
              && Body[0].GetType() == typeof(DoWhile))
              ? ToCodeFormat.AlwaysBraces
              : ToCodeFormat.Normal
              );

            // if the body is a single statement that ends in a do-while, then we
            // will need to wrap the body in curly-braces to get around an IE bug
            if (Body != null && Body.EncloseBlock(EncloseBlockType.SingleDoWhile))
            {
                bodyFormat = ToCodeFormat.AlwaysBraces;
            }

            string bodyString = (
              Body == null
              ? string.Empty
              : Body.ToCode(bodyFormat)
              );
            if (bodyString.Length == 0)
            {
                sb.Append(';');
            }
            else
            {
                // if the first character could be interpreted as a continuation
                // of the "do" keyword, then we need to add a space
                if (JSScanner.StartsWithIdentifierPart(bodyString))
                {
                    sb.Append(' ');
                }
                sb.Append(bodyString);

                // if there is no body, we need a semi-colon
                // OR if we didn't always wrap in braces AND we require a separator, we need a semi-colon.
                // and make sure it doesn't already end in a semicolon -- we don't want two in a row.
                if (Body == null
                    || (bodyFormat != ToCodeFormat.AlwaysBraces && Body.RequiresSeparator && !bodyString.EndsWith(";", StringComparison.Ordinal)))
                {
                    sb.Append(';');
                }
            }
            // add a space for readability for pretty-print mode
            if (Parser.Settings.OutputMode == OutputMode.MultipleLines && Parser.Settings.IndentSize > 0)
            {
                sb.Append(' ');
            }
            sb.Append("while(");
            sb.Append(Condition.ToCode());
            sb.Append(")");
            return sb.ToString();
        }

        public override void CleanupNodes()
        {
            base.CleanupNodes();

            if (Parser.Settings.EvalLiteralExpressions
                && Parser.Settings.IsModificationAllowed(TreeModifications.EvaluateNumericExpressions))
            {
                // if the condition is a constant, we can simplify things
                ConstantWrapper constantCondition = Condition as ConstantWrapper;
                if (constantCondition != null && constantCondition.IsNotOneOrPositiveZero)
                {
                    try
                    {
                        // the condition is a constant, so it is always either true or false
                        // we can replace the condition with a one or a zero -- only one byte
                        Condition = new ConstantWrapper(constantCondition.ToBoolean() ? 1 : 0, PrimitiveType.Number, null, Parser);
                    }
                    catch (InvalidCastException)
                    {
                        // ignore any invalid cast errors
                    }
                }
            }
        }
    }
}
