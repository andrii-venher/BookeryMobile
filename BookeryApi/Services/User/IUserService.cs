using System;
using System.Threading.Tasks;
using BookeryApi.Services.Common;

namespace BookeryApi.Services.User
{
    public interface IUserService : IBaseService
    {
        Task<Domain.Models.User> Get();
        Task<Domain.Models.User> GetByEmail(string email);
        Task<Domain.Models.User> GetById(Guid id);
    }
}