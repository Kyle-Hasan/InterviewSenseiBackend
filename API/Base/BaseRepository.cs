using API.Data;
using API.Users;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
namespace API.Base;

public abstract class BaseRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _entities;
    public BaseRepository(AppDbContext appDbContext)
    {
        this._context = appDbContext;
        this._entities = _context.Set<T>();
    }
    
    public async Task Delete(T entity,AppUser user)
    {
        _entities.Remove(entity);
        
        await SaveChanges(user);
    }

    public async Task<T> Save(T entity,AppUser user)
    {
        
        if (entity.Id == 0)
        {
            _entities.Add(entity);
        }
        else if (entity.Id > 0)
        {
            _entities.Update(entity);
        }
        await SaveChanges(user);
        return entity;
    }
    
    public async Task<bool> SaveChanges(AppUser user)
    {
        return await _context.SaveChangesAsync(true,user) > 0;
    }

    public async Task<List<T>> getAllCreatedBy(AppUser user)
    {
        
        IQueryable<T> results = _entities.Where(x=> x.CreatedById == user.Id);
        return await results.ToListAsync();
    }

    public async Task<T> getById(int id)
    {
        return await _entities.FindAsync(id);
    }

    public T updateObjectFields(T updatedObject, T existingObject)
    {
        Type type = updatedObject.GetType();
        var properties = type.GetProperties();
        updatedObject.CreatedBy = existingObject.CreatedBy;
        updatedObject.CreatedDate = existingObject.CreatedDate;
        updatedObject.CreatedById = existingObject.CreatedById;
        for (int i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var newVal = property.GetValue(updatedObject);
            property.SetValue(existingObject, newVal);
            
        }
        return existingObject;
    }

}