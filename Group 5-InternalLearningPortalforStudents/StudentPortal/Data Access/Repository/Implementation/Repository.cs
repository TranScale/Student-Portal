using Microsoft.EntityFrameworkCore;
using StudentPortal.Data;
using StudentPortal.Data_Access.Repository.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPortal.Data_Access.Repository.Implementation
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly StudentPortalContext _context;
        private readonly DbSet<T>? _dbSet;
        public Repository(StudentPortalContext context, DbSet<T> _dbSet)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _dbSet!.ToListAsync();
        }

        public async Task<T?> GetById(int id)
        {
            return await _dbSet!.FindAsync(id);
        }

        public async Task Add(T entity)
        {
            await _dbSet!.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Update(T entity)
        {
            _dbSet!.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var entity = await _dbSet!.FindAsync(id);
            if(entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }


    }
}
