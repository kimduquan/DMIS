namespace ContentRepository.Storage.Search
{
	//////////////////////////////////////// Node Attributes ////////////////////////////////////////

	public enum StringAttribute
	{
		Name = 1,
		Path = 2,
		ETag = 5,
		LockToken = 6,
        ChangedData = 7
	}

	public enum IntAttribute
	{
		Id = 101,
        IsDeleted = 102,
		Index = 103,
		Locked = 104,
		LockType = 105,
		LockTimeout = 106,
		MajorVersion = 107,
		MinorVersion = 108,
		FullTextRank = 109,
		ParentId = 110,
		LockedById = 111,
		CreatedById = 112,
		ModifiedById = 113,
		LastMinorVersionId = 114,
		LastMajorVersionId = 115,
        IsInherited = 116,
        IsSystem = 117,
        ClosestSecurityNodeId = 118,
        SavingState = 119
    }

	public enum DateTimeAttribute
	{
		LockDate = 201,
		LastLockUpdate = 202,
		CreationDate = 203,
		ModificationDate = 204
	}

	public enum ReferenceAttribute
	{
		Parent = 301,
		LockedBy = 302,
		CreatedBy = 303,
		ModifiedBy = 304
	}

	public enum NodeAttribute
	{
		//-- StringAttribute
		Name = 1,
		Path = 2,
		ETag = 5,
		LockToken = 6,

		//-- IntAttribute
		Id = 101,
        IsDeleted = 102,
		Index = 103,
		Locked = 104,
		LockType = 105,
		LockTimeout = 106,
		MajorVersion = 107,
		MinorVersion = 108,
		FullTextRank = 109,
		ParentId = 110,
		LockedById = 111,
		CreatedById = 112,
		ModifiedById = 113,
		LastMinorVersionId = 114,
		LastMajorVersionId = 115,
        IsInherited = 116,
        IsSystem = 117,
        ClosestSecurityNodeId = 118,
        SavingState = 119,

		//-- DateTimeAttribute
		LockDate = 201,
		LastLockUpdate = 202,
		CreationDate = 203,
		ModificationDate = 204,

		//-- ReferenceAttribute
		Parent = 301,
		LockedBy = 302,
		CreatedBy = 303,
		ModifiedBy = 304
	}


	//////////////////////////////////////// Operators, Orders ////////////////////////////////////////

	public enum ChainOperator
	{
		And,
		Or
	}

	public enum StringOperator
	{
		Equal,
		NotEqual,
		LessThan,
		GreaterThan,
		LessThanOrEqual,
		GreaterThanOrEqual,
		StartsWith,
		EndsWith,
		Contains
	}

	public enum ValueOperator
	{
		Equal,
		NotEqual,
		LessThan,
		GreaterThan,
		LessThanOrEqual,
		GreaterThanOrEqual
	}

	public enum OrderDirection
	{
		Asc,
		Desc
	}

}
