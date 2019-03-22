﻿# CacheRepository
一个简易、轻量的模拟In-Memory Repository的存在

## 思路

一般来说经典的缓存其使用场景适用于读多写少的情况。使用的方式也很简单，一般通用的模式是访问某个缓存值如果存在则从缓存中直接获取，否则从数据库读取并存入缓存中；

然而很多时候我们的业务并没有这般简单清晰，可能是读多写操作同样也很多，这个时候更是需要考虑降低后端数据库的压力。

CacheRepository提供了一种思路，混合了经典缓存的使用方式和类似Entity Framework中Repository的操作方式；简单来说就是，大方向上这里仍然会存在一个缓存，业务逻辑所有的读写都从这里过，包括插入，更新和删除操作；这些写的动作，会通过Repository中的同步逻辑回写到磁盘上去，具体的回写方式，则是通过抽象出一个IWriteBack接口以方便实现；

## 主要类型

 - `IRepository`
 
   模拟了一个简单的内存repository
 - `IShardable`
 
   考虑到并发访问控制锁粒度过高的问题，这里提供了分片的机制，以试图减轻并发时锁的开销
 - `IWriteBack`
 
   终归整个操作都是在内存中完成的，因此如果业务数据很重要，那么刷回到磁盘上将是必不可少的

大致上仍然会看到`CacheRepository<TKey, TValue, TShardKey>`这样的设计，我已经说了这里仍然会存在一个大的缓存（缓存的特质会体现得更多一点），所以跟repository不同，这个大缓存中是只能放入一种类型的对象的（`TKey`，`TValue`）

而repository的特质则体现在，所有业务的写操作实际都发生在这个大缓存上。我期望能够像`sql server`一样，书写sql语句就能在内存中执行各种增删改查的工作；这里虽然大缓存中只有一种类型的对象，但是如果能够很方便支持谓词条件的查询也会让业务逻辑层的编码轻松不少；

## Roadmap

- 如上面所说，缓存对象支持谓词条件的查询
    现有的方式因为是是现在`Dictionary`上，所以采用`Func<TValue, bool>`进行过滤的时候算法的执行效率是O(n)，在缓存对象数目较少的情况下还行，如果多了则效率低下
