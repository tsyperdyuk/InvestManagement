using InvestManagement.Application.Contracts;

namespace InvestManagement.Application.Common.Interfaces;

public interface IOutboxWriter
{
    void Add(TransactionCreated message);

    void Add(ProcessTransaction message);
}
