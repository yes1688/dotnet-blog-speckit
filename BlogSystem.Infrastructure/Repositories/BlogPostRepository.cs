using BlogSystem.Core.Entities;
using BlogSystem.Core.Interfaces;
using BlogSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlogSystem.Infrastructure.Repositories
{
    /// <summary>
    /// BlogPost 專用 Repository，自動載入導航屬性
    /// </summary>
    public class BlogPostRepository : Repository<BlogPost>
    {
        public BlogPostRepository(BlogDbContext context) : base(context) { }

        public override async Task<IEnumerable<BlogPost>> FindAsync(Expression<Func<BlogPost, bool>> predicate)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(predicate)
                .ToListAsync();
        }

        public override async Task<BlogPost?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
