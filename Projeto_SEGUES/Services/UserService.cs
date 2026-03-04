using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;

namespace Projeto_SEGUES.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    
    public UserService(AppDbContext context) => _context = context;
    
}