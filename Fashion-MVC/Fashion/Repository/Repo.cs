
using Microsoft.EntityFrameworkCore;
using App_Web.Models;

namespace App_Web.Repository;

public class Repo<T> : IRepo<T> where T: class
{
    private readonly AppDbContext _db;

    public Repo(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(T entity)
    {
        await _db.Set<T>().AddAsync(entity);
    }

    public void Delete(T entity)
    {
        _db.Set<T>().Remove(entity);
    }

    public T Get(int Id)
    {
        return _db.Set<T>().Find(Id);
    }

    public async Task<IEnumerable<T>> Gets()
    {
        return await _db.Set<T>().ToListAsync();
    }

    public void Update(T entity)
    {
        _db.Set<T>().Update(entity);
    }
}
