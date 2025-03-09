namespace Centazio.Core.Tests.Misc;

public class GlobalEnumerableExtensionMethodsTests {

  [Test] public void Test_AddAndReturn() {
    var list = new List<string>();
    Assert.That(list.AddAndReturn(nameof(Test_AddAndReturn)), Is.EqualTo(nameof(Test_AddAndReturn)));
    Assert.That(list.AddAndReturn(nameof(GlobalEnumerableExtensionMethodsTests)), Is.EqualTo(nameof(GlobalEnumerableExtensionMethodsTests)));
    Assert.That(list, Is.EquivalentTo(new List<string> {nameof(Test_AddAndReturn), nameof(GlobalEnumerableExtensionMethodsTests)}));
  }

  [Test] public void Test_Deconstruct() {
    var lst = Enumerable.Range(1, 100).ToList();
    var (first1, rest1) = lst;
    var (first2, second2, rest2) = lst;
    var (first3, second3, third3, rest3) = lst;
    
    Assert.That(first1, Is.EqualTo(1));
    Assert.That(rest1, Is.EquivalentTo(Enumerable.Range(2, 99)));
    
    Assert.That(first2, Is.EqualTo(1));
    Assert.That(second2, Is.EqualTo(2));
    Assert.That(rest2, Is.EquivalentTo(Enumerable.Range(3, 98)));
    
    Assert.That(first3, Is.EqualTo(1));
    Assert.That(second3, Is.EqualTo(2));
    Assert.That(third3, Is.EqualTo(3));
    Assert.That(rest3, Is.EquivalentTo(Enumerable.Range(4, 97)));
  }
  
  [Test] public async Task Test_Synchronous() {
    var result = await Enumerable.Range(1, 10).Select(DoTask).Synchronous();
    Assert.That(result, Is.EquivalentTo(Enumerable.Range(1, 10)));
    
    Task<int> DoTask(int echo) => Task.FromResult(echo);
  }
  
  [Test] public async Task Test_Synchronous_with_abort() {
    var result = await Enumerable.Range(1, 10).Select(DoTask).Synchronous(val => val > 3);
    Assert.That(result, Is.EquivalentTo(Enumerable.Range(1, 4)));
    
    Task<int> DoTask(int echo) => Task.FromResult(echo);
  }
  
  [Test] public void Test_Chunk() {
    var chunks = GlobalEnumerableExtensionMethods.Chunk(Enumerable.Range(0, 100), 10).ToList();
    Assert.That(chunks, Has.Count.EqualTo(10));
    Assert.That(chunks[0], Is.EquivalentTo(Enumerable.Range(0, 10)));
    Assert.That(chunks[^1], Is.EquivalentTo(Enumerable.Range(90, 10)));
  }
  
  [Test] public async Task Test_ChunkedSynchronousCall() {
    var results = await Enumerable.Range(0, 100).Select(Task.FromResult).ChunkedSynchronousCall(10);
    Assert.That(results, Is.EquivalentTo(Enumerable.Range(0, 100)));
  }
}