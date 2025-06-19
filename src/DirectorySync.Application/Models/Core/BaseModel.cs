using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Models.Core;

public abstract class BaseModel
    {
        public DirectoryGuid Id { get; }
            
        protected BaseModel(DirectoryGuid id)
        {
            ArgumentNullException.ThrowIfNull(id);
            
            Id = id;
        }
    
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }
            
            var compareTo = obj as BaseModel;
    
            if (ReferenceEquals(this, compareTo))
            {
                return true;
            }
    
            if (ReferenceEquals(null, compareTo))
            {
                return false;
            }
    
            return Id == compareTo.Id;
        }
    
        public static bool operator ==(BaseModel a, BaseModel b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return true;
            }
    
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }
    
            return a.Equals(b);
        }
    
        public static bool operator !=(BaseModel a, BaseModel b)
        {
            return !(a == b);
        }
    
        public override int GetHashCode()
        {
            unchecked
            {
                return GetType().GetHashCode() * 907 + Id.GetHashCode();
            }
        }
    }
