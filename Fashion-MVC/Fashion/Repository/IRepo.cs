namespace App_Web.Repository;

public interface IRepo<T> where T : class
{
    Task<IEnumerable<T>> Gets();
    T Get(int Id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
