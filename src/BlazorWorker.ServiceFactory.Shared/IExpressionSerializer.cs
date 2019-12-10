using System.Linq.Expressions;

namespace BlazorWorker.BackgroundServiceFactory
{
    public interface IExpressionSerializer
    {
        string Serialize(Expression expr);

        Expression Deserialize(string exprString);
    }
}
