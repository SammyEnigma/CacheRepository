using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CacheRepository.Tests
{
    public class Tests_Single_ValType
    {
        private readonly SingleShardRepository _repository;

        public Tests_Single_ValType()
        {
            _repository = new SingleShardRepository();
            _repository.Init();
        }
    }
}
