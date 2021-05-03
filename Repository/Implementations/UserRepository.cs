using Microsoft.EntityFrameworkCore;
using Microsoft.SallyBot.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SallyBot.Repository.Implementations
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
