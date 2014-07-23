#region License
/*
Copyright (c) 2013-2014 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System;
using System.Linq.Expressions;

namespace SharpRpc.Codecs.Expressions
{
    public unsafe class ExpressionCodec : IManualCodec<Expression>
    {
        private const ExpressionType NullExpressionType = (ExpressionType)(-1);

        private readonly IManualCodec<ExpressionType> expressionTypeCodec;

        private readonly BinaryExpressionSubcodec binarySubcodec;
        private readonly ConditionalExpressionSubcodec conditionalSubcodec;
        private readonly ConstantExpressionSubcodec constantSubcodec;
        private readonly InvocationExpressionSubcodec invocationSubcodec;
        private readonly LambdaExpressionSubcodec lambdaSubcodec;
        private readonly MethodCallExpressionSubcodec methodCallSubcodec;
        private readonly ParameterExpressionSubcodec parameterSubcodec;
        private readonly UnaryExpressionSubcodec unarySubcodec;

        public ExpressionCodec(ICodecContainer codecContainer)
        {
            expressionTypeCodec = codecContainer.GetManualCodecFor<ExpressionType>();

            binarySubcodec = new BinaryExpressionSubcodec(this, codecContainer);
            conditionalSubcodec = new ConditionalExpressionSubcodec(this, codecContainer);
            constantSubcodec = new ConstantExpressionSubcodec(this, codecContainer);
            invocationSubcodec= new InvocationExpressionSubcodec(this, codecContainer);
            lambdaSubcodec = new LambdaExpressionSubcodec(this, codecContainer);
            methodCallSubcodec = new MethodCallExpressionSubcodec(this, codecContainer);
            parameterSubcodec = new ParameterExpressionSubcodec(this, codecContainer);
            unarySubcodec= new UnaryExpressionSubcodec(this, codecContainer);
        }

        public Type Type { get { return typeof(Expression); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public  int CalculateSize(Expression value)
        {
            if (value == null)
                return expressionTypeCodec.CalculateSize(NullExpressionType);
            var subcodec = GetSubcodec(value.NodeType);
            return subcodec.CalculateSize(value);
        }

        public void Encode(ref byte* data, Expression value)
        {
            if (value == null)
            {
                expressionTypeCodec.Encode(ref data, NullExpressionType);
                return;
            }   
            var subcodec = GetSubcodec(value.NodeType);
            subcodec.Encode(ref data, value);
        }

        public Expression Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var nodeType = expressionTypeCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            if (nodeType == NullExpressionType)
                return null;
            var subcodec = GetSubcodec(nodeType);
            return subcodec.Decode(nodeType, ref data, ref remainingBytes, doNotCheckBounds);
        }

        private IExpressionSubcodec GetSubcodec(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Power:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Assign:
                case ExpressionType.AddAssign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.SubtractAssignChecked:
                    return binarySubcodec;
                case ExpressionType.ArrayLength:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Negate:
                case ExpressionType.UnaryPlus:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.Decrement:
                case ExpressionType.Increment:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.OnesComplement:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                    return unarySubcodec;
                case ExpressionType.Call:
                    return methodCallSubcodec;
                case ExpressionType.Conditional:
                    return conditionalSubcodec;
                case ExpressionType.Constant:
                    return constantSubcodec;
                case ExpressionType.Invoke:
                    return invocationSubcodec;
                case ExpressionType.Lambda:
                    return lambdaSubcodec;
                case ExpressionType.ListInit:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitListInit((ListInitExpression)expression);
                case ExpressionType.MemberAccess:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitMember((MemberExpression)expression);
                case ExpressionType.MemberInit:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitMemberInit((MemberInitExpression)expression);
                case ExpressionType.New:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitNew((NewExpression)expression);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitNewArray((NewArrayExpression)expression);
                case ExpressionType.Parameter:
                    return parameterSubcodec;
                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitTypeBinary((TypeBinaryExpression)expression);
                case ExpressionType.Block:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitBlock((BlockExpression)expression);
                case ExpressionType.DebugInfo:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitDebugInfo((DebugInfoExpression)expression);
                case ExpressionType.Dynamic:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitDynamic((DynamicExpression)expression);
                case ExpressionType.Default:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitDefault((DefaultExpression)expression);
                case ExpressionType.Extension:
                    throw new NotSupportedException("ExpressionType.Extension is not supported");
                case ExpressionType.Goto:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitGoto((GotoExpression)expression);
                case ExpressionType.Index:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitIndex((IndexExpression)expression);
                case ExpressionType.Label:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitLabel((LabelExpression)expression);
                case ExpressionType.RuntimeVariables:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitRuntimeVariables((RuntimeVariablesExpression)expression);
                case ExpressionType.Loop:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitLoop((LoopExpression)expression);
                case ExpressionType.Switch:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitSwitch((SwitchExpression)expression);
                case ExpressionType.Throw:
                    throw new NotSupportedException("ExpressionType.Throw is not supported");
                case ExpressionType.Try:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
                    //VisitTry((TryExpression)expression);
                case ExpressionType.Unbox:
                    throw new NotSupportedException("ExpressionType.Unbox is not supported");
                default:
                    throw new NotSupportedException(string.Format("ExpressionType.{0} is not supported", expressionType));
            }
        }
    }
}