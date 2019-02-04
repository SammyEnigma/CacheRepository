using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CacheRepository.Tests
{
    public class Tests_Multiple_RefType
    {
        private readonly MultipleShardsRepository _repository;

        public Tests_Multiple_RefType()
        {
            _repository = new MultipleShardsRepository();
            _repository.Init();
        }
    }
}
