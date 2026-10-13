# Proofs

Overzicht van alle proof-tests in `src/Unshackle.Tests/Proofs/`. Gegenereerd door `claude/tools/proofs-doc.py`; niet met de hand bewerken.

Een proof is een `*.before.cs` (compileert tegen de oude bibliotheek) plus `*.after.cs` (verwachte uitvoer, compileert tegen de nieuwe)
of `*.manual.txt` (verwachte Manual-reden). `ProofTests` en `CombinedProofTests` draaien ze; `_Pending/` bevat bekende fouten met een taak.
Kolom *Wat het bewijst*: bij Fixed de eerste regel die verandert (voor → na), bij Manual de assertion en de reden.

Totaal: 802 proofs (zonder `_Pending`).

## AutoMapper

### _Combined (3)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `map-call-global` | Fixed | `CreateMap<Order, OrderDto>() .ForMember(d => d.Id, o => o.MapFrom(s => s.Id + 1));` → `} public static class OrderProfileExtensions` |
| `map-call-profile` | Fixed | `CreateMap<Order, OrderDto>(); public Mapping.OrderDto Get(Mapping.Order order) => _mapper.Map<Mapping.OrderDto…` → `} public static class OrderProfileExtensions` |
| `map-into-profile` | Fixed | `CreateMap<Order, OrderDto>() .ForMember(d => d.Note, o => o.Ignore());` → `} }` |

### AddAutoMapper (4)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `mapper-injected` | Manual |  — reden: IMapper is still used |
| `parameter` | Fixed | `services.AddAutoMapper(profiles);` → `_ = profiles;` |
| `result-used` | Manual |  — reden: The result of AddAutoMapper is used |
| `simple` | Fixed | `services.AddAutoMapper(typeof(OrderProfile).Assembly);` →  |

### CreateMap (7)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `collection` | Manual | `CreateMap<Order, OrderDto>();` — reden: is a collection |
| `constructor` | Manual | `CreateMap<Order, OrderDto>();` — reden: has no public parameterless constructor |
| `conversion` | Manual | `CreateMap<Order, OrderDto>();` — reden: (AutoMapper converts) |
| `flattening` | Manual | `CreateMap<Order, OrderDto>();` — reden: has no member with the same name |
| `internal-types` | Fixed | `CreateMap<Order, OrderDto>();` → `internal static class OrderProfileExtensions {` |
| `options` | Manual | `CreateMap<Order, OrderDto>().ReverseMap();` — reden: is not a CreateMap<A, B>() without options |
| `simple` | Fixed | `CreateMap<Product, ProductDto>(); CreateMap<Tag, TagDto>();` → `public static class ProductProfileExtensions {` |

### ForMemberIgnore (5)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `mixed` | Fixed | `CreateMap<Order, OrderDto>(); CreateMap<Line, LineDto>().ForMember(d => d.Total, o => o.Ignore());` → `public static class OrderProfileExtensions {` |
| `reverse-map` | Manual | `CreateMap<Order, OrderDto>().ForMember(d => d.Note, o => o.Ignore()).ForMember(d => d.Rank, o => o.Ignore()).R…` — reden: is not a CreateMap<A, B>() without options |
| `simple` | Fixed | `CreateMap<Product, ProductDto>() .ForMember(d => d.Secret, o => o.Ignore())` → `public static class ProductProfileExtensions {` |
| `string-name` | Manual | `CreateMap<Order, OrderDto>().ForMember(d => d.Rank, o => o.Ignore()).ForMember("Note", o => o.Ignore());` — reden: is not a CreateMap<A, B>() without options |
| `unmatched` | Manual | `CreateMap<Order, OrderDto>().ForMember(d => d.Note, o => o.Ignore());` — reden: OrderDto.Rank has no member with the same name in Order |

### ForMemberMapFrom (7)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `collection` | Manual | `CreateMap<Order, OrderDto>()` — reden: is a collection |
| `conversion` | Manual | `CreateMap<Order, OrderDto>()` — reden: (AutoMapper converts) |
| `ignore-and-nullable` | Fixed | `CreateMap<Order, OrderDto>() .ForMember(d => d.Note, o => o.Ignore())` → `public static class OrderProfileExtensions {` |
| `null-deref` | Manual | `CreateMap<Order, OrderDto>()` — reden: which may be null |
| `outside-profile` | Manual | `CreateMap<Order, OrderDto>()` — reden: is not available outside the profile |
| `resolver-form` | Manual | `CreateMap<Order, OrderDto>()` — reden: is not a CreateMap<A, B>() without options |
| `simple` | Fixed | `CreateMap<Order, OrderDto>() .ForMember(d => d.Total, o => o.MapFrom(s => s.Price * s.Quantity))` → `public static class OrderProfileExtensions {` |

### MapCall (7)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `collection` | Manual | `CreateMap<Order, OrderDto>();` — reden: maps a collection |
| `existing-method` | Fixed | `public OrderDto Get(Order order) => _mapper.Map<OrderDto>(order); public OrderDto First(Order[] orders) => thi…` → `public OrderDto Get(Order order) => order.ToOrderDto(); public OrderDto First(Order[] orders) => orders[0].ToO…` |
| `manual-profile` | Manual | `CreateMap<Order, OrderDto>().ReverseMap();` — reden: No CreateMap<Order, OrderDto> in a profile this fix rewrites |
| `maybe-null` | Manual |  — reden: may be null |
| `object-source` | Manual |  — reden: no type of its own |
| `receiver-call` | Manual |  — reden: not called as mapper.Map on a plain field |
| `unknown-map` | Manual |  — reden: No CreateMap<Order, OrderDto> in a profile this fix rewrites |

### MapInto (7)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `collection` | Manual |  — reden: maps a collection |
| `existing-method` | Fixed | `public OrderDto Update(Order order, OrderDto dto) => _mapper.Map(order, dto); public void Fill(Order[] orders,…` → `public OrderDto Update(Order order, OrderDto dto) => order.MapInto(dto); public void Fill(Order[] orders, Orde…` |
| `maybe-null-destination` | Manual |  — reden: destination of Map may be null |
| `maybe-null-source` | Manual |  — reden: source of Map may be null |
| `object-source` | Manual |  — reden: no type of its own |
| `receiver-call` | Manual |  — reden: plain field, property or variable |
| `unknown-map` | Manual |  — reden: No CreateMap<Order, OrderDto> |

## FluentAssertions

### _Combined (11)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `all-satisfy-be` | Fixed | `orders.Should().AllSatisfy(o => o.Total.Should().Be(20m));` → `orders.ShouldNotBeEmpty(); foreach (var o in orders)` |
| `all-satisfy-other` | Fixed | `orders.Should().AllSatisfy(o => o.Note.Should().NotBeNull();` → `orders.ShouldNotBeEmpty(); foreach (var o in orders)` |
| `lambda-overload-one-run` | Fixed | `ctx.Should().Be(context); maybe2.HasValue.Should().BeTrue();` → `ctx.ShouldBe(context); maybe2.HasValue.ShouldBeTrue();` |
| `manual-and-fixed` | Fixed | `y.Should().Be(1); y.Should().Be(2);` → `y.ShouldBe(1); y.ShouldBe(2);` |
| `mixed` | Fixed | `order.Id.Should().Be(1); order.Total.Should().BeGreaterThan(10m).And.BeLessThan(30m);` → `order.Id.ShouldBe(1); order.Total.ShouldBeGreaterThan(10m);` |
| `scope-using-block` | Fixed | `{ x.Should().Be(1);` → `x.ShouldSatisfyAllConditions( () => { x.ShouldBe(1); },` |
| `scope-using-var` | Fixed | `y.Should().Be(2); name.Should().StartWith("a");` → `y.ShouldSatisfyAllConditions( () => { y.ShouldBe(2); },` |
| `usings` | Fixed | `x.Should().HaveCount(2); y.Should().Contain(3);` → `x.Count().ShouldBe(2); y.ShouldContain(3);` |
| `usings-existing` | Fixed | `x.Should().HaveCount(2); y.Should().Contain(3);` → `x.Count().ShouldBe(2); y.ShouldContain(3);` |
| `usings-sorted` | Fixed | `Calc.Sum(values).Should().Be(3);` → `Calc.Sum(values).ShouldBe(3);` |
| `usings-unsorted` | Fixed | `Calc.Sum(values).Should().Be(3);` → `Calc.Sum(values).ShouldBe(3);` |

### AllSatisfy (15)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and-chain` | Manual | `x.Should().NotBeEmpty().And.AllSatisfy(i => Check(i));` — reden: is not in the form |
| `async` | Manual | `x.Should().AllSatisfy(async i => await System.Threading.Tasks.Task.CompletedTask);` — reden: is async |
| `because` | Manual | `x.Should().AllSatisfy(i => Check(i), "they are valid");` — reden: because message |
| `because-args` | Manual | `x.Should().AllSatisfy(i => Check(i), "because {0}", 1);` — reden: because message |
| `block` | Fixed | `x.Should().AllSatisfy(i => });` → `x.ShouldNotBeEmpty(); foreach (var i in x)` |
| `edge-expression-lambda` | Fixed | `items.Should().AllSatisfy(x => Check(x));` → `items.ShouldNotBeEmpty(); foreach (var x in items)` |
| `member-subject` | Fixed | `h.Items.Should().AllSatisfy(i => Check(i));` → `h.Items.ShouldNotBeEmpty(); foreach (var i in h.Items)` |
| `method-group` | Manual | `x.Should().AllSatisfy(Check);` — reden: not an inline lambda |
| `not-in-block` | Manual | `if (x.Count > 0) x.Should().AllSatisfy(i => Check(i));` — reden: not directly in a block |
| `nullable` | Fixed | `x.Should().AllSatisfy(i => Check(i));` → `x.ShouldNotBeEmpty(); foreach (var i in x)` |
| `return` | Manual | `x.Should().AllSatisfy(i =>` — reden: contains a return |
| `simple` | Fixed | `x.Should().AllSatisfy(i => Check(i));` → `x.ShouldNotBeEmpty(); foreach (var i in x)` |
| `subject-call` | Fixed | `f().Should().AllSatisfy(i => Check(i));` → `var subject = f(); subject.ShouldNotBeEmpty();` |
| `typed-parameter` | Fixed | `x.Should().AllSatisfy((int i) => Check(i));` → `x.ShouldNotBeEmpty(); foreach (int i in x)` |
| `used` | Manual | `x.Should().AllSatisfy(i => Check(i)).And.NotBeEmpty();` — reden: is used |

### AndChain (46)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `items.Should().NotBeEmpty().And.HaveCount(2);` — reden: AssertionScope |
| `be-of-type` | Fixed | `obj.Should().NotBeNull().And.BeOfType<string>();` → `obj.ShouldNotBeNull(); obj.ShouldBeOfType<string>();` |
| `be-same-as` | Fixed | `a.Should().NotBeNull().And.BeSameAs(b);` → `a.ShouldNotBeNull(); a.ShouldBeSameAs(b);` |
| `because` | Fixed | `items.Should().NotBeEmpty("needed").And.Contain(3, "three");` → `items.ShouldNotBeEmpty("needed"); items.ShouldContain(3, "three");` |
| `contain-keys` | Fixed | `x.Should().ContainKeys("a", "b");` → `x.ShouldContainKey("a"); x.ShouldContainKey("b");` |
| `contain-keys-and` | Fixed | `x.Should().ContainKeys("a", "b").And.NotBeEmpty();` → `x.ShouldContainKey("a"); x.ShouldContainKey("b");` |
| `contain-keys-collection` | Manual | `x.Should().ContainKeys(new List<string> { "a", "b" }, "because");` — reden: with a collection argument |
| `contain-keys-read-only` | Fixed | `x.Should().ContainKeys("a", "b");` → `x.ContainsKey("a").ShouldBeTrue(); x.ContainsKey("b").ShouldBeTrue();` |
| `contain-keys-subject-call` | Fixed | `Get().Should().ContainKeys("a", "b");` → `var subject = Get(); subject.ShouldContainKey("a");` |
| `contain-keys-values` | Fixed | `contextData.Should().ContainKeys("key1", "key2").And.ContainValues("value1", "value2");` → `contextData.ShouldContainKey("key1"); contextData.ShouldContainKey("key2");` |
| `contain-value` | Fixed | `x.Should().ContainValue(1);` → `x.Values.ShouldContain(1);` |
| `contain-value-because` | Fixed | `x.Should().ContainValue(1, "because {0}", "reason");` → `x.Values.ShouldContain(1, string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", "rea…` |
| `contain-value-read-only` | Fixed | `x.Should().ContainValue(1);` → `x.Values.ShouldContain(1);` |
| `contain-values` | Fixed | `x.Should().ContainValues(1, 2);` → `x.Values.ShouldContain(1); x.Values.ShouldContain(2);` |
| `contain-values-collection` | Manual | `x.Should().ContainValues(new[] { 1, 2 });` — reden: with a collection argument |
| `dates` | Fixed | `a.Should().BeAfter(b).And.BeBefore(c);` → `a.ShouldBeGreaterThan(b); a.ShouldBeLessThan(c);` |
| `edge-multiline` | Fixed | `items.Should() .NotBeEmpty()` → `items.ShouldNotBeEmpty(); items.Count.ShouldBe(2);` |
| `equal-length-approximately` | Fixed | `x.Should().NotBeEmpty().And.Equal(1, 2); s.Should().NotBeNull().And.HaveLength(3);` → `x.ShouldNotBeEmpty(); x.ShouldBe(new int[] { 1, 2 });` |
| `equivalent-collection` | Fixed | `a.Should().NotBeEmpty().And.BeEquivalentTo(b);` → `a.ShouldNotBeEmpty(); a.ShouldBe(b, ignoreOrder: true);` |
| `greater-less` | Fixed | `order.Total.Should().BeGreaterThan(20m).And.BeLessThan(30m); order.Total.Should().BeGreaterThan(1m, "above").A…` → `order.Total.ShouldBeGreaterThan(20m); order.Total.ShouldBeLessThan(30m);` |
| `lambda` | Manual | `Action a = () => x.Should().BeGreaterThan(1).And.BeLessThan(5);` — reden: is used |
| `link-because-args` | Fixed | `items.Should().NotBeEmpty().And.HaveCount(2, "of {0}", 2);` → `items.ShouldNotBeEmpty(); items.Count.ShouldBe(2, string.Format(System.Globalization.CultureInfo.InvariantCult…` |
| `manual-link` | Manual | `x.Should().NotBeNull().And.BeGreaterThan(1);` — reden: of the .And chain: |
| `member-subject` | Fixed | `_order.Lines.Should().NotBeEmpty().And.HaveCount(1);` → `_order.Lines.ShouldNotBeEmpty(); _order.Lines.Count.ShouldBe(1);` |
| `no-block` | Manual | `if (items.Count > 0) items.Should().NotBeEmpty().And.HaveCount(2);` — reden: not directly in a block |
| `not-contain-keys` | Fixed | `x.Should().NotContainKeys("a", "b");` → `x.ShouldNotContainKey("a"); x.ShouldNotContainKey("b");` |
| `not-contain-value` | Fixed | `x.Should().NotContainValue(1);` → `x.Values.ShouldNotContain(1);` |
| `not-contain-values` | Fixed | `x.Should().NotContainValues(1, 2);` → `x.Values.ShouldNotContain(1); x.Values.ShouldNotContain(2);` |
| `nullable` | Fixed | `name.Should().NotBeNull().And.Be("Ann");` → `name.ShouldNotBeNull(); name.ShouldBe("Ann");` |
| `simple` | Fixed | `items.Should().NotBeEmpty().And.HaveCount(2); name.Should().NotBeNull().And.Be("Ann");` → `items.ShouldNotBeEmpty(); items.Count.ShouldBe(2);` |
| `subject-call` | Fixed | `GetItems().Should().NotBeEmpty().And.HaveCount(2);` → `var subject = GetItems(); subject.ShouldNotBeEmpty();` |
| `subject-call-twice` | Fixed | `GetItems().Should().NotBeEmpty().And.HaveCount(2); GetItems().Should().Contain(1).And.HaveCount(2);` → `var subject = GetItems(); subject.ShouldNotBeEmpty();` |
| `subject-indexer` | Fixed | `lists[0].Should().NotBeEmpty().And.HaveCount(2);` → `var subject = lists[0]; subject.ShouldNotBeEmpty();` |
| `three-links` | Fixed | `items.Should().NotBeEmpty().And.HaveCount(2).And.Contain(1);` → `items.ShouldNotBeEmpty(); items.Count.ShouldBe(2);` |
| `unsupported-link` | Manual | `items.Should().NotBeEmpty().And.HaveElementAt(0, 1);` — reden: which no pattern rewrites |
| `used` | Manual | `var c = items.Should().NotBeEmpty().And.HaveCount(2);` — reden: is used |
| `which` | Manual | `items.Should().NotBeEmpty().And.ContainSingle().Which.Should().Be(1);` — reden: is used |
| `whose-value` | Fixed | `x.Should().ContainKey("a").WhoseValue.Should().Be(1);` → `x.ShouldContainKey("a"); x["a"].ShouldBe(1);` |
| `whose-value-and` | Fixed | `x.Should().ContainKey("a").WhoseValue.Should().BeGreaterThan(0).And.BeLessThan(10);` → `x.ShouldContainKey("a"); x["a"].ShouldBeGreaterThan(0);` |
| `whose-value-because` | Fixed | `x.Should().ContainKey("a", "it is there").WhoseValue.Should().Be(1, "it is one");` → `x.ShouldContainKey("a", "it is there"); x["a"].ShouldBe(1, "it is one");` |
| `whose-value-computed-key` | Manual | `x.Should().ContainKey(Key()).WhoseValue.Should().Be(1);` — reden: the key would be evaluated twice |
| `whose-value-key-variable` | Fixed | `x.Should().ContainKey(key).WhoseValue.Should().Be(1);` → `x.ShouldContainKey(key); x[key].ShouldBe(1);` |
| `whose-value-no-indexer` | Manual | `x.Should().ContainKey("a").WhoseValue.Should().Be(1);` — reden: has no indexer by the key |
| `whose-value-other` | Fixed | `x.Should().ContainKey("a").WhoseValue.Should().StartWith("b");` → `x.ShouldContainKey("a"); x["a"].ShouldStartWith("b", Shouldly.Case.Sensitive);` |
| `whose-value-read-only` | Fixed | `x.Should().ContainKey("a").WhoseValue.Should().Be(1);` → `x.ContainsKey("a").ShouldBeTrue(); x["a"].ShouldBe(1);` |
| `whose-value-subject-call` | Fixed | `Get().Should().ContainKey("a").WhoseValue.Should().Be(1);` → `var subject2 = Get(); subject2.ShouldContainKey("a");` |

### AssertionScope (4)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `await` | Manual | `x.Should().Be(1);` — reden: Shouldly has no scope |
| `context` | Manual | `x.Should().Be(1);` — reden: Shouldly has no scope |
| `discard` | Manual | `x.Should().Be(1);` — reden: Shouldly has no scope |
| `other-statement` | Manual | `x.Should().Be(1);` — reden: Shouldly has no scope |

### BeApproximately (9)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `d.Should().BeApproximately(1.5, 0.01);` — reden: AssertionScope |
| `because` | Fixed | `d.Should().BeApproximately(1.5, 0.01, "rounding");` → `d.ShouldBe(1.5, 0.01, "rounding");` |
| `because-args` | Fixed | `d.Should().BeApproximately(1.5, 0.01, "of {0}", 1);` → `d.ShouldBe(1.5, 0.01, string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 1));` |
| `close-to` | Fixed | `t.Should().BeCloseTo(u, TimeSpan.FromSeconds(2)); o.Should().BeCloseTo(p, TimeSpan.FromSeconds(2), "clock");` → `t.ShouldBe(u, TimeSpan.FromSeconds(2)); o.ShouldBe(p, TimeSpan.FromSeconds(2), "clock");` |
| `int-close-to` | Manual | `i.Should().BeCloseTo(5, 1);` — reden: tolerance only for |
| `not` | Fixed | `d.Should().NotBeApproximately(1.5, 0.01); f.Should().NotBeApproximately(1.5f, 0.01f, "far");` → `System.Math.Abs(d - 1.5).ShouldBeGreaterThan(0.01); System.Math.Abs(f - 1.5f).ShouldBeGreaterThan(0.01f, "far"…` |
| `nullable` | Fixed | `d.Should().BeApproximately(1.5, 0.01); t.Should().BeCloseTo(u, TimeSpan.FromSeconds(1));` → `d.ShouldNotBeNull().ShouldBe(1.5, 0.01); t.ShouldNotBeNull().ShouldBe(u, TimeSpan.FromSeconds(1));` |
| `nullable-not` | Manual | `d.Should().NotBeApproximately(1.5, 0.01);` — reden: nullable subject |
| `simple` | Fixed | `d.Should().BeApproximately(1.5, 0.01); f.Should().BeApproximately(1.5f, 0.01f);` → `d.ShouldBe(1.5, 0.01); f.ShouldBe(1.5f, 0.01f);` |

### BeEmpty (16)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and-chain` | Manual | `text.Should().NotBeNull().And.NotBeEmpty();` — reden: is not in the form |
| `be-empty-list` | Fixed | `list.Should().BeEmpty();` → `list.ShouldBeEmpty();` |
| `because` | Fixed | `list.Should().BeEmpty("fresh order"); text.Should().NotBeEmpty("must be filled");` → `list.ShouldBeEmpty("fresh order"); text.ShouldNotBeEmpty("must be filled");` |
| `because-args` | Fixed | `text.Should().BeEmpty("because {0}", 1);` → `text.ShouldBeEmpty(string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", 1));` |
| `chain` | Manual | `text.Should().NotBeEmpty().And.StartWith("a");` — reden: is used |
| `dictionary` | Fixed | `map.Should().BeEmpty(); map.Should().NotBeEmpty();` → `map.ShouldBeEmpty(); map.ShouldNotBeEmpty();` |
| `edge-await-subject` | Fixed | `(await load()).Should().BeEmpty();` → `(await load()).ShouldBeEmpty();` |
| `edge-linq-subject` | Fixed | `items.Where(x => x < 0).Should().BeEmpty();` → `items.Where(x => x < 0).ShouldBeEmpty();` |
| `enumerable` | Fixed | `items.Should().BeEmpty(); array.Should().NotBeEmpty();` → `items.ShouldBeEmpty(); array.ShouldNotBeEmpty();` |
| `guid` | Fixed | `id.Should().NotBeEmpty(); id.Should().BeEmpty("it is new");` → `id.ShouldNotBe(Guid.Empty); id.ShouldBe(Guid.Empty, "it is new");` |
| `guid-nullable-not-empty` | Manual | `id.Should().NotBeEmpty();` — reden: fails on null |
| `lambda` | Fixed | `Action act = () => text.Should().BeEmpty();` → `Action act = () => text.ShouldBeEmpty();` |
| `not-be-empty-list` | Fixed | `list.Should().NotBeEmpty();` → `list.ShouldNotBeEmpty();` |
| `nullable` | Fixed | `x.Should().BeEmpty();` → `x.ShouldBeEmpty();` |
| `string` | Fixed | `text.Should().BeEmpty(); text.Should().NotBeEmpty();` → `text.ShouldBeEmpty(); text.ShouldNotBeEmpty();` |
| `value-used` | Manual | `var result = text.Should().BeEmpty();` — reden: is used |

### BeEquivalentToObject (37)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and-chain` | Manual | `a.Should().NotBeNull().And.BeEquivalentTo(b);` — reden: is not in the form |
| `anonymous` | Manual | `a.Should().BeEquivalentTo(new { Id = 1 });` — reden: Different compile-time types |
| `base-vs-derived` | Manual | `a.Should().BeEquivalentTo(b);` — reden: Different compile-time types |
| `because` | Fixed | `a.Should().BeEquivalentTo(b, "same dto");` → `a.ShouldBeEquivalentTo(b, "same dto");` |
| `because-args` | Fixed | `a.Should().BeEquivalentTo(b, "reason {0}", 1);` → `a.ShouldBeEquivalentTo(b, string.Format(System.Globalization.CultureInfo.InvariantCulture, "reason {0}", 1));` |
| `both-new` | Fixed | `new Dto { Id = 1 }.Should().BeEquivalentTo(new Dto { Id = 1 });` → `new Dto { Id = 1 }.ShouldBeEquivalentTo(new Dto { Id = 1 });` |
| `collection` | Manual | `a.Should().BeEquivalentTo(b);` — reden: no leaf, record or struct |
| `collection-because-args` | Fixed | `a.Should().BeEquivalentTo(b, "id {0}", 1);` → `a.ShouldBe(b, ignoreOrder: true, string.Format(System.Globalization.CultureInfo.InvariantCulture, "id {0}", 1)…` |
| `collection-element-types` | Manual | `a.Should().BeEquivalentTo(b);` — reden: other items than |
| `collection-ints` | Fixed | `a.Should().BeEquivalentTo(b);` → `a.ShouldBe(b, ignoreOrder: true);` |
| `collection-member` | Manual | `a.Should().BeEquivalentTo(b);` — reden: is a collection |
| `collection-nullable-enum` | Fixed | `a.Should().BeEquivalentTo(b);` → `a.ShouldBe(b, ignoreOrder: true);` |
| `collection-record` | Fixed | `actual.Should().BeEquivalentTo(expected); actual.Should().BeEquivalentTo(new[] { new Item(2, null), new Item(1…` → `actual.ShouldBe(expected, ignoreOrder: true); actual.ShouldBe(new[] { new Item(2, null), new Item(1, "a"), new…` |
| `collection-record-nested` | Manual | `actual.Should().BeEquivalentTo(expected);` — reden: no leaf, record or struct |
| `collection-record-unsealed` | Manual | `actual.Should().BeEquivalentTo(expected);` — reden: no leaf, record or struct |
| `collection-strings-because` | Fixed | `a.Should().BeEquivalentTo(b, "any order");` → `a.ShouldBe(b, ignoreOrder: true, "any order");` |
| `collection-struct` | Fixed | `pairs.Should().BeEquivalentTo(expectedPairs); stamps.Should().BeEquivalentTo(expectedStamps);` → `pairs.ShouldBe(expectedPairs, ignoreOrder: true); stamps.ShouldBe(expectedStamps, ignoreOrder: true);` |
| `cyclic` | Manual | `a.Should().BeEquivalentTo(b);` — reden: cyclic object graph |
| `different-types` | Manual | `a.Should().BeEquivalentTo(b);` — reden: Different compile-time types |
| `edge-array-literal` | Fixed | `result.Should().BeEquivalentTo(new[] { 1, 2 });` → `result.ShouldBe(new[] { 1, 2 }, ignoreOrder: true);` |
| `edge-collection-params` | Fixed | `numbers.Select(n => n.ToString()).Should().BeEquivalentTo("1", "2");` → `numbers.Select(n => n.ToString()).ShouldBe(new string[] { "1", "2" }, ignoreOrder: true);` |
| `equals-member` | Manual | `a.Should().BeEquivalentTo(b);` — reden: overrides Equals |
| `equals-override` | Manual | `a.Should().BeEquivalentTo(b);` — reden: overrides Equals |
| `lambda` | Fixed | `System.Action act = () => a.Should().BeEquivalentTo(b);` → `System.Action act = () => a.ShouldBeEquivalentTo(b);` |
| `nested` | Fixed | `a.Should().BeEquivalentTo(b);` → `a.ShouldBeEquivalentTo(b);` |
| `new-expectation` | Fixed | `a.Should().BeEquivalentTo(new Dto { Id = 1, Name = "x" });` → `a.ShouldBeEquivalentTo(new Dto { Id = 1, Name = "x" });` |
| `no-members` | Manual | `a.Should().BeEquivalentTo(b);` — reden: no public members |
| `not-equivalent` | Manual | `a.Should().NotBeEquivalentTo(b);` — reden: no Shouldly equivalent |
| `object-member` | Manual | `a.Should().BeEquivalentTo(b);` — reden: not a plain class or struct |
| `object-subject` | Manual | `a.Should().BeEquivalentTo(b);` — reden: not a plain class or struct |
| `params-array` | Fixed | `names.Should().BeEquivalentTo(expected); names.Should().BeEquivalentTo(new[] { "A", "B" });` → `names.ShouldBe(expected, ignoreOrder: true); names.ShouldBe(new[] { "A", "B" }, ignoreOrder: true);` |
| `params-multi-line` | Fixed | `lines.Should().BeEquivalentTo( "last");` → `lines.ShouldBe(new string[] {` |
| `params-strings` | Fixed | `names.Should().BeEquivalentTo("A", "B"); names.Should().BeEquivalentTo("A");` → `names.ShouldBe(new string[] { "A", "B" }, ignoreOrder: true); names.ShouldBe(new string[] { "A" }, ignoreOrder…` |
| `record` | Fixed | `a.Should().BeEquivalentTo(b);` → `a.ShouldBeEquivalentTo(b);` |
| `sealed` | Fixed | `a.Should().BeEquivalentTo(b);` → `a.ShouldBeEquivalentTo(b);` |
| `simple` | Fixed | `a.Should().BeEquivalentTo(b);` → `a.ShouldBeEquivalentTo(b);` |
| `value-used` | Manual | `var c = a.Should().BeEquivalentTo(b);` — reden: is used |

### BeEquivalentToString (9)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and-chain` | Manual | `s.Should().NotBeNull().And.BeEquivalentTo("ab");` — reden: is not in the form |
| `because` | Fixed | `s.Should().BeEquivalentTo("ab", "names are case-free");` → `s.ShouldBe("ab", "names are case-free", StringCompareShould.IgnoreCase);` |
| `because-args` | Fixed | `s.Should().BeEquivalentTo("ab", "reason {0}", 1);` → `s.ShouldBe("ab", string.Format(System.Globalization.CultureInfo.InvariantCulture, "reason {0}", 1), StringComp…` |
| `chain` | Manual | `s.Should().BeEquivalentTo("ab").And.HaveLength(2);` — reden: is used |
| `edge-interpolated` | Fixed | `text.Should().BeEquivalentTo($"hello {name}");` → `text.ShouldBe($"hello {name}", StringCompareShould.IgnoreCase);` |
| `lambda` | Fixed | `Action act = () => s.Should().BeEquivalentTo("ab");` → `Action act = () => s.ShouldBe("ab", StringCompareShould.IgnoreCase);` |
| `not-equivalent` | Manual | `s.Should().NotBeEquivalentTo("ab");` — reden: no Shouldly equivalent |
| `simple` | Fixed | `s.Should().BeEquivalentTo("ab");` → `s.ShouldBe("ab", StringCompareShould.IgnoreCase);` |
| `value-used` | Manual | `var c = s.Should().BeEquivalentTo("ab");` — reden: is used |

### BeGreaterLess (21)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `because` | Fixed | `x.Should().BeGreaterThan(1, "it grew"); x.Should().BeInRange(1, 5, "it is small");` → `x.ShouldBeGreaterThan(1, "it grew"); x.ShouldBeInRange(1, 5, "it is small");` |
| `because-args` | Fixed | `x.Should().BeGreaterThan(1, "it is {0}", 1);` → `x.ShouldBeGreaterThan(1, string.Format(System.Globalization.CultureInfo.InvariantCulture, "it is {0}", 1));` |
| `because-variable` | Manual | `x.Should().BeGreaterThan(1, why, 1);` — reden: becauseArgs whose because is no constant format string |
| `byte-short` | Fixed | `b.Should().BeGreaterThan(1); s.Should().BeLessOrEqualTo(10, "small");` → `b.ShouldBeGreaterThan((byte)1); s.ShouldBeLessThanOrEqualTo((short)10, "small");` |
| `comparable` | Fixed | `t.Should().BeGreaterThan(TimeSpan.Zero); v.Should().BeLessThan(new Version(2, 0));` → `t.ShouldBeGreaterThan(TimeSpan.Zero); v.ShouldBeLessThan(new Version(2, 0));` |
| `dates` | Fixed | `a.Should().BeAfter(b); a.Should().BeOnOrAfter(b);` → `a.ShouldBeGreaterThan(b); a.ShouldBeGreaterThanOrEqualTo(b);` |
| `dates-nullable` | Manual | `a.Should().BeAfter(b);` — reden: nullable |
| `edge-double` | Fixed | `ratio.Should().BeGreaterThan(0.5);` → `ratio.ShouldBeGreaterThan(0.5);` |
| `edge-interpolated-because` | Fixed | `age.Should().BeLessThan(150, $"{name} is human");` → `age.ShouldBeLessThan(150, $"{name} is human");` |
| `implicit-conversion` | Fixed | `doc.Should().BeGreaterThan(0); doc.Should().BeLessThanOrEqualTo(7, "small");` → `doc.ShouldBeGreaterThan((Doc)0); doc.ShouldBeLessThanOrEqualTo((Doc)7, "small");` |
| `in-range` | Fixed | `x.Should().BeInRange(1, 5); x.Should().NotBeInRange(6, 9);` → `x.ShouldBeInRange(1, 5); x.ShouldNotBeInRange(6, 9);` |
| `lambda` | Fixed | `Action a = () => x.Should().BeGreaterThan(1);` → `Action a = () => x.ShouldBeGreaterThan(1);` |
| `named-because` | Manual | `x.Should().BeGreaterThan(1, because: "it is positive");` — reden: named arguments |
| `negative-double` | Fixed | `d.Should().BeNegative();` → `(d < 0).ShouldBeTrue();` |
| `negative-float` | Fixed | `f.Should().BeNegative("it dropped"); (a - b).Should().BeNegative();` → `(f < 0).ShouldBeTrue("it dropped"); ((a - b) < 0).ShouldBeTrue();` |
| `nullable` | Manual | `x.Should().BeGreaterThan(1);` — reden: nullable or has an unknown type |
| `sign` | Fixed | `x.Should().BePositive(); d.Should().BePositive("it dropped");` → `x.ShouldBePositive(); d.ShouldBePositive("it dropped");` |
| `sign-byte` | Manual | `b.Should().BePositive();` — reden: only exists for |
| `simple` | Fixed | `x.Should().BeGreaterThan(1); x.Should().BeGreaterThanOrEqualTo(2);` → `x.ShouldBeGreaterThan(1); x.ShouldBeGreaterThanOrEqualTo(2);` |
| `value-type-differs` | Manual | `score.Should<int>().BeGreaterThan(5);` — reden: differs from subject type |
| `value-used` | Manual | `var c = x.Should().BeGreaterThan(1);` — reden: is used |

### BeNull (15)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and-chain` | Manual | `text.Should().NotBeEmpty().And.NotBeNull();` — reden: is not in the form |
| `be-null` | Fixed | `text.Should().BeNull();` → `text.ShouldBeNull();` |
| `because` | Fixed | `text.Should().BeNull("not set yet"); text.Should().NotBeNull("must be set");` → `text.ShouldBeNull("not set yet"); text.ShouldNotBeNull("must be set");` |
| `because-args` | Fixed | `text.Should().NotBeNull("missing {0}", id);` → `text.ShouldNotBeNull(string.Format(System.Globalization.CultureInfo.InvariantCulture, "missing {0}", id));` |
| `chain` | Manual | `text.Should().NotBeNull().And.NotBeEmpty();` — reden: is used |
| `collection` | Fixed | `list.Should().NotBeNull(); array.Should().BeNull();` → `list.ShouldNotBeNull(); array.ShouldBeNull();` |
| `edge-expression-bodied` | Fixed | `public void Test(object? value) => value.Should().BeNull();` → `public void Test(object? value) => value.ShouldBeNull();` |
| `edge-not-null-and` | Manual | `text.Should().NotBeNull().And.Be("a");` — reden: is used |
| `edge-null-conditional` | Fixed | `uri?.Host.Should().BeNull();` → `uri?.Host.ShouldBeNull();` |
| `edge-null-forgiving` | Fixed | `text!.Should().NotBeNull();` → `text!.ShouldNotBeNull();` |
| `lambda` | Fixed | `Action act = () => text.Should().BeNull();` → `Action act = () => text.ShouldBeNull();` |
| `not-be-null` | Fixed | `item.Should().NotBeNull();` → `item.ShouldNotBeNull();` |
| `nullable-value` | Fixed | `number.Should().BeNull(); flag.Should().NotBeNull("why");` → `number.ShouldBeNull(); flag.ShouldNotBeNull("why");` |
| `struct` | Fixed | `entry.Should().NotBeNull("the check should be registered");` → `((object)entry).ShouldNotBeNull("the check should be registered");` |
| `value-used` | Manual | `var constraint = text.Should().NotBeNull();` — reden: is used |

### BeOfType (12)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and` | Manual | `obj.Should().BeOfType<Dto>().And.NotBeNull();` — reden: is used |
| `assertion-scope` | Manual | `obj.Should().BeOfType<Dto>();` — reden: AssertionScope |
| `because` | Fixed | `obj.Should().BeOfType<Dto>("mapped");` → `obj.ShouldBeOfType<Dto>("mapped");` |
| `because-args` | Fixed | `obj.Should().BeOfType<Dto>("of {0}", 1);` → `obj.ShouldBeOfType<Dto>(string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 1));` |
| `nullable` | Fixed | `obj.Should().BeOfType<Dto>(); Dto dto = obj.Should().BeOfType<Dto>().Which;` → `obj.ShouldBeOfType<Dto>(); Dto dto = obj.ShouldBeOfType<Dto>();` |
| `nullable-type` | Fixed | `value.Should().BeOfType(Type.GetType(name, throwOnError: true));` → `value.ShouldBeOfType(Type.GetType(name, throwOnError: true)!);` |
| `simple` | Fixed | `obj.Should().BeOfType<Dto>(); obj.Should().BeAssignableTo<object>();` → `obj.ShouldBeOfType<Dto>(); obj.ShouldBeAssignableTo<object>();` |
| `type-argument` | Fixed | `obj.Should().BeOfType(typeof(Dto)); obj.Should().NotBeAssignableTo(typeof(string), "no");` → `obj.ShouldBeOfType(typeof(Dto)); obj.ShouldNotBeAssignableTo(typeof(string), "no");` |
| `type-subject` | Manual | `typeof(Dto).Should().BeAssignableTo<object>();` — reden: on a Type |
| `which` | Fixed | `var dto = obj.Should().BeOfType<Dto>().Which; var asObject = obj.Should().BeAssignableTo<object>("any").Subjec…` → `var dto = obj.ShouldBeOfType<Dto>(); var asObject = obj.ShouldBeAssignableTo<object>("any");` |
| `which-chain` | Fixed | `string name = obj.Should().BeOfType<Dto>().Which.Name;` → `string name = obj.ShouldBeOfType<Dto>().Name;` |
| `which-multiple` | Fixed | `Dto a = obj.Should().BeOfType<Dto>().Which, b = new Dto();` → `Dto a = obj.ShouldBeOfType<Dto>(), b = new Dto();` |

### BeSameAs (7)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `a.Should().BeSameAs(b);` — reden: AssertionScope |
| `because` | Fixed | `a.Should().BeSameAs(b, "cached");` → `a.ShouldBeSameAs(b, "cached");` |
| `because-args` | Fixed | `a.Should().BeSameAs(b, "id {0}", 1);` → `a.ShouldBeSameAs(b, string.Format(System.Globalization.CultureInfo.InvariantCulture, "id {0}", 1));` |
| `not` | Fixed | `a.Should().NotBeSameAs(b);` → `a.ShouldNotBeSameAs(b);` |
| `nullable` | Fixed | `a.Should().BeSameAs(b);` → `a.ShouldBeSameAs(b);` |
| `result-used` | Manual | `var c = a.Should().BeSameAs(b).And;` — reden: is used |
| `simple` | Fixed | `a.Should().BeSameAs(b);` → `a.ShouldBeSameAs(b);` |

### BeTrueFalse (13)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-helper` | Manual | `candidate.Should().Throw<AssertionFailedException>();` — reden: Throw<XunitException> |
| `be-false` | Fixed | `(a == b).Should().BeFalse();` → `(a == b).ShouldBeFalse();` |
| `be-true` | Fixed | `flag.Should().BeTrue();` → `flag.ShouldBeTrue();` |
| `because` | Fixed | `flag.Should().BeTrue("must be paid");` → `flag.ShouldBeTrue("must be paid");` |
| `because-args` | Fixed | `flag.Should().BeTrue("x {0}", y);` → `flag.ShouldBeTrue(string.Format(System.Globalization.CultureInfo.InvariantCulture, "x {0}", y));` |
| `chain` | Manual | `flag.Should().BeTrue().And.NotBe(false);` — reden: is used |
| `edge-has-value` | Fixed | `value.HasValue.Should().BeTrue();` → `value.HasValue.ShouldBeTrue();` |
| `edge-interpolated-because` | Fixed | `ok.Should().BeTrue($"{name} must be valid");` → `ok.ShouldBeTrue($"{name} must be valid");` |
| `edge-linq-subject` | Fixed | `items.Any(x => x > 1).Should().BeFalse();` → `items.Any(x => x > 1).ShouldBeFalse();` |
| `edge-pattern-subject` | Fixed | `(value is string { Length: > 0 }).Should().BeTrue();` → `(value is string { Length: > 0 }).ShouldBeTrue();` |
| `nullable` | Fixed | `flag.Should().BeTrue(); flag.Should().BeFalse("why");` → `flag.ShouldBe(true); flag.ShouldBe(false, "why");` |
| `own-shouldbe-in-scope` | Fixed | `done.Should().BeTrue();` → `done.ShouldBeTrue();` |
| `value-used` | Manual | `var c = flag.Should().BeFalse();` — reden: is used |

### CatchAssertionException (4)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `catch-clause` | Fixed |  → `catch (ShouldAssertException) {` |
| `catch-uses-exception` | Manual |  — reden: uses the exception: add a catch for Shouldly.ShouldAssertException by hand |
| `retry-filter` | Fixed | `if (!(e is AssertionFailedException) \|\| i == attempts)` → `if (!(e is AssertionFailedException \|\| e is ShouldAssertException) \|\| i == attempts)` |
| `when-filter` | Fixed | `catch (Exception e) when (e is AssertionFailedException)` → `catch (Exception e) when (e is AssertionFailedException \|\| e is ShouldAssertException)` |

### Contain (20)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and-chain` | Manual | `x.Should().NotBeEmpty().And.Contain(2);` — reden: is not in the form |
| `because` | Fixed | `x.Should().Contain(2, "it was added"); text.Should().Contain("ab", "the prefix");` → `x.ShouldContain(2, "it was added"); text.ShouldContain("ab", Shouldly.Case.Sensitive, "the prefix");` |
| `because-args` | Fixed | `x.Should().Contain(2, "it was {0}", "added");` → `x.ShouldContain(2, string.Format(System.Globalization.CultureInfo.InvariantCulture, "it was {0}", "added"));` |
| `because-variable` | Manual | `x.Should().Contain(2, why, "added");` — reden: becauseArgs whose because is no constant format string |
| `chain` | Manual | `x.Should().Contain(2).And.HaveCount(3);` — reden: is used |
| `dictionary` | Manual | `x.Should().Contain(new KeyValuePair<string, int>("a", 1));` — reden: Dictionary: |
| `edge-generic-list` | Fixed | `items.Should().Contain(item);` → `items.ShouldContain(item);` |
| `edge-interpolated` | Fixed | `text.Should().Contain($"id={id}");` → `text.ShouldContain($"id={id}", Shouldly.Case.Sensitive);` |
| `enumerable` | Fixed | `x.Should().Contain("a"); arr.Should().Contain("b");` → `x.ShouldContain("a"); arr.ShouldContain("b");` |
| `items` | Manual | `x.Should().Contain(new[] { 1, 2 });` — reden: several items |
| `lambda` | Fixed | `System.Action act = () => x.Should().Contain(1);` → `System.Action act = () => x.ShouldContain(1);` |
| `list` | Fixed | `x.Should().Contain(2);` → `x.ShouldContain(2);` |
| `named-because` | Manual | `x.Should().Contain(2, because: "it was added");` — reden: named arguments |
| `not-contain` | Fixed | `x.Should().NotContain(2); text.Should().NotContain("bad");` → `x.ShouldNotContain(2); text.ShouldNotContain("bad", Shouldly.Case.Sensitive);` |
| `nullable-expected` | Fixed | `ex.Message.Should().Contain(typeof(NullableExpectedCase).FullName);` → `ex.Message.ShouldContain(typeof(NullableExpectedCase).FullName!, Shouldly.Case.Sensitive);` |
| `nullable-string` | Fixed | `text.Should().Contain("ab");` → `text.ShouldNotBeNull().ShouldContain("ab", Shouldly.Case.Sensitive);` |
| `occurrence` | Manual | `text.Should().Contain("ab", Exactly.Twice());` — reden: occurrence constraint |
| `predicate` | Fixed | `x.Should().Contain(e => e.StartsWith("a")); x.Should().NotContain(e => e.Length > 3);` → `x.ShouldContain(e => e.StartsWith("a")); x.ShouldNotContain(e => e.Length > 3);` |
| `string` | Fixed | `text.Should().Contain("ab");` → `text.ShouldContain("ab", Shouldly.Case.Sensitive);` |
| `value-used` | Manual | `var result = x.Should().Contain(2);` — reden: is used |

### ContainEquivalentOf (15)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `x.Should().ContainEquivalentOf("a");` — reden: AssertionScope |
| `because` | Fixed | `x.Should().ContainEquivalentOf(2, "it was added"); x.Should().NotContainEquivalentOf(5, "it was {0}", "removed…` → `x.ShouldContain(2, "it was added"); x.ShouldNotContain(5, string.Format(System.Globalization.CultureInfo.Invar…` |
| `class` | Manual | `x.Should().ContainEquivalentOf(new Dto { Id = 1 });` — reden: is no leaf, record or struct of leaf members |
| `dictionary` | Manual | `x.Should().ContainEquivalentOf(new KeyValuePair<string, int>("a", 1));` — reden: is no leaf, record or struct of leaf members |
| `different-type` | Manual | `x.Should().ContainEquivalentOf(2L);` — reden: the elements are Int32 |
| `nullable-subject` | Fixed | `x.Should().ContainEquivalentOf("a"); x.Should().NotContainEquivalentOf("b");` → `x.ShouldNotBeNull().ShouldContain("a"); x.ShouldNotContain("b");` |
| `nullable-value` | Fixed | `x.Should().ContainEquivalentOf(at); y.Should().ContainEquivalentOf((int?)null);` → `x.ShouldContain(at); y.ShouldContain((int?)null);` |
| `options` | Manual | `x.Should().ContainEquivalentOf("a", o => o.Excluding(s => s.Length));` — reden: equivalency options have no Shouldly equivalent |
| `record-list-member` | Manual | `x.Should().ContainEquivalentOf(item);` — reden: is no leaf, record or struct of leaf members |
| `record-struct` | Fixed | `x.Should().ContainEquivalentOf(new Point(1, 2));` → `x.ShouldContain(new Point(1, 2));` |
| `record-unsealed` | Manual | `x.Should().NotContainEquivalentOf(item);` — reden: is no leaf, record or struct of leaf members |
| `sealed-record` | Fixed | `x.Should().ContainEquivalentOf(item); x.Should().NotContainEquivalentOf(new Item(1, "a", DateTime.MinValue));` → `x.ShouldContain(item); x.ShouldNotContain(new Item(1, "a", DateTime.MinValue));` |
| `simple` | Fixed | `x.Should().ContainEquivalentOf(2); x.Should().NotContainEquivalentOf(5);` → `x.ShouldContain(2); x.ShouldNotContain(5);` |
| `strings` | Fixed | `x.Should().ContainEquivalentOf("abc");` → `x.ShouldContain("abc");` |
| `which` | Manual | `var found = x.Should().ContainEquivalentOf("a").Which;` — reden: is used |

### ContainKey (20)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and-chain` | Manual | `x.Should().NotBeEmpty().And.ContainKey("a");` — reden: is not in the form |
| `because` | Fixed | `x.Should().ContainKey("a", "it was added");` → `x.ShouldContainKey("a", "it was added");` |
| `because-args` | Fixed | `x.Should().ContainKey("a", "because {0}", 1);` → `x.ShouldContainKey("a", string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", 1));` |
| `chain` | Manual | `x.Should().ContainKey("a").And.NotBeEmpty();` — reden: is used |
| `contain-value` | Manual | `x.Should().ContainValue(1);` — reden: has no Shouldly equivalent |
| `contain-values` | Manual | `x.Should().ContainValues(1, 2);` — reden: has no Shouldly equivalent |
| `dictionary` | Fixed | `x.Should().ContainKey("a");` → `x.ShouldContainKey("a");` |
| `edge-nested-generic` | Fixed | `map.Should().ContainKey("a");` → `map.ShouldContainKey("a");` |
| `enumerable-pairs` | Manual | `x.Should().ContainKey("a");` — reden: has no ContainsKey/Contains(key) method |
| `headers` | Fixed | `response.Headers.Should().ContainKey("Access-Control-Allow-Origin"); response.Headers.Should().NotContainKey("…` → `response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeTrue(); response.Headers.Contains("Vary").Sho…` |
| `idictionary` | Fixed | `x.Should().ContainKey("a");` → `x.ShouldContainKey("a");` |
| `keys` | Manual | `x.Should().ContainKeys("a", "b");` — reden: several keys |
| `lambda` | Fixed | `System.Action a = () => x.Should().ContainKey("a");` → `System.Action a = () => x.ShouldContainKey("a");` |
| `not-contain-key` | Fixed | `x.Should().NotContainKey("a");` → `x.ShouldNotContainKey("a");` |
| `not-contain-keys` | Manual | `x.Should().NotContainKeys("a", "b");` — reden: several keys |
| `not-contain-value` | Manual | `x.Should().NotContainValue(1);` — reden: has no Shouldly equivalent |
| `nullable` | Fixed | `x.Should().ContainKey("a");` → `x.ShouldNotBeNull().ShouldContainKey("a");` |
| `read-only` | Fixed | `x.Should().ContainKey(1);` → `x.ContainsKey(1).ShouldBeTrue();` |
| `read-only-not-contain` | Fixed | `Metadata.Should().NotContainKey("a", "it was removed");` → `Metadata.ContainsKey("a").ShouldBeFalse("it was removed");` |
| `which` | Manual | `var v = x.Should().ContainKey("a").WhoseValue;` — reden: is used |

### ContainSingle (11)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `x.Should().ContainSingle();` — reden: AssertionScope |
| `because` | Fixed | `x.Should().ContainSingle("one"); x.Should().ContainSingle(i => i > 2, "one");` → `x.ShouldHaveSingleItem("one"); x.ShouldContain(i => i > 2, 1, "one");` |
| `because-args` | Fixed | `x.Should().ContainSingle("of {0}", 1);` → `x.ShouldHaveSingleItem(string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 1));` |
| `edge-subject` | Fixed | `var name = names.Should().ContainSingle().Subject;` → `var name = names.ShouldHaveSingleItem();` |
| `enumerable` | Fixed | `x.Should().ContainSingle(s => s == "a");` → `x.ShouldContain(s => s == "a", 1);` |
| `expression-variable` | Manual | `var one = x.Should().ContainSingle(p).Which;` — reden: Expression variable |
| `nullable` | Fixed | `x.Should().ContainSingle(); int one = x.Should().ContainSingle().Which;` → `x.ShouldNotBeNull().ShouldHaveSingleItem(); int one = x.ShouldHaveSingleItem();` |
| `predicate` | Fixed | `x.Should().ContainSingle(i => i > 2);` → `x.ShouldContain(i => i > 2, 1);` |
| `simple` | Fixed | `x.Should().ContainSingle();` → `x.ShouldHaveSingleItem();` |
| `which` | Fixed | `var only = x.Should().ContainSingle().Which; var big = x.Should().ContainSingle(d => d.Price > 5m, "big").Whic…` → `var only = x.ShouldHaveSingleItem(); var big = x.Where(d => d.Price > 5m).ShouldHaveSingleItem("big");` |
| `which-chain` | Fixed | `decimal price = x.Should().ContainSingle().Which.Price;` → `decimal price = x.ShouldHaveSingleItem().Price;` |

### CustomAssertion (4)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `extension` | Manual | `result.Should().BeFailure();` — reden: Custom assertion `ResultAssertions.BeFailure` |
| `in-assertion-scope` | Manual | `result.Should().BeFailure();` — reden: Custom assertion `ScopedResultAssertions.BeFailure` |
| `should-extension` | Manual | `order.Should().BeCancelled("it was refused");` — reden: Custom assertion `CancelOrderAssertions.BeCancelled` |
| `subclass` | Manual |  — reden: Custom assertion `OrderAssertions.BeShipped` |

### DirectMapping (9)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `be-one-of` | Fixed | `x.Should().BeOneOf(1, 2);` → `x.ShouldBeOneOf(1, 2);` |
| `be-one-of-convert` | Fixed | `x.Should().BeOneOf(1, 2);` → `x.ShouldBeOneOf(1, 2);` |
| `be-one-of-enumerable` | Manual | `x.Should().BeOneOf(ok, "valid");` — reden: with a collection |
| `be-subset-of` | Fixed | `x.Should().BeSubsetOf(all, "because");` → `x.ShouldBeSubsetOf(all, "because");` |
| `contain-in-order` | Manual | `x.Should().ContainInOrder(1, 2);` — reden: no Shouldly equivalent |
| `have-value` | Fixed | `x.Should().HaveValue();` → `x.ShouldNotBeNull();` |
| `not-have-value` | Fixed | `x.Should().NotHaveValue("not set");` → `x.ShouldBeNull("not set");` |
| `unique` | Fixed | `x.Should().OnlyHaveUniqueItems();` → `x.ShouldBeUnique();` |
| `unique-key` | Manual | `x.Should().OnlyHaveUniqueItems(s => s.Length);` — reden: key selector |

### EqualSequence (16)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and` | Manual | `x.Should().Equal(y).And.HaveCount(3);` — reden: .And |
| `assertion-scope` | Manual | `x.Should().Equal(y);` — reden: AssertionScope |
| `because` | Fixed | `x.Should().Equal(y, "order matters");` → `x.ShouldBe(y, ignoreOrder: false, "order matters");` |
| `because-args` | Fixed | `x.Should().Equal(y, "of {0}", 2);` → `x.ShouldBe(y, ignoreOrder: false, string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 2…` |
| `collection-expression` | Fixed | `s.Should().Equal(["a", "b"]); x.Should().Equal([1]);` → `s.ShouldBe(new string[] { "a", "b" }); x.ShouldBe(new int[] { 1 });` |
| `collection-expression-spread` | Manual | `s.Should().Equal([.. t, "b"]);` — reden: spread |
| `dictionary` | Fixed | `x.Should().Equal(y);` → `x.ShouldBe(y, ignoreOrder: true);` |
| `dictionary-because` | Fixed | `x.Should().Equal(y, "the {0} counts", "node");` → `x.ShouldBe(y, ignoreOrder: true, string.Format(System.Globalization.CultureInfo.InvariantCulture, "the {0} cou…` |
| `dictionary-class-values` | Manual | `x.Should().Equal(y);` — reden: are no leaf, record or struct of leaves |
| `dictionary-pair-list` | Manual | `x.Should().Equal(y);` — reden: The expectation is no dictionary |
| `multi-line` | Fixed | `lines.Should().Equal( "last");` → `lines.ShouldBe(new string[] {` |
| `nullable` | Fixed | `x.Should().Equal(y);` → `x.ShouldBe(y);` |
| `params` | Fixed | `x.Should().Equal(1, 2, 3); s.Should().Equal("a", "b");` → `x.ShouldBe(new long[] { 1, 2, 3 }); s.ShouldBe(new string[] { "a", "b" });` |
| `params-array` | Fixed | `x.Should().Equal(y); x.Should().Equal(new[] { 1 });` → `x.ShouldBe(y); x.ShouldBe(new[] { 1 });` |
| `predicate` | Manual | `x.Should().Equal(y, (a, b) => a.ToString() == b);` — reden: predicate |
| `simple` | Fixed | `x.Should().Equal(y);` → `x.ShouldBe(y);` |

### EquivalentOptions (33)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `block-body` | Manual | `a.Should().BeEquivalentTo(b, o => { return o.Excluding(x => x.Id); });` — reden: The options are no inline lambda |
| `collection-excluding` | Manual | `a.Should().BeEquivalentTo(b, o => o.Excluding(x => x.Id));` — reden: Excluding(x => x.Id): ShouldBeEquivalentTo compares every member |
| `comparing-by-value` | Manual | `a.Should().BeEquivalentTo(b, o => o.ComparingByValue<Dto>());` — reden: ComparingByValue(): it changes how values are compared |
| `excluding` | Manual | `a.Should().BeEquivalentTo(b, options => options.Excluding(x => x.Id));` — reden: Excluding(x => x.Id): ShouldBeEquivalentTo compares every member and cannot skip any |
| `excluding-missing-members` | Fixed | `a.Should().BeEquivalentTo(b, o => o.ExcludingMissingMembers());` → `a.ShouldBeEquivalentTo(b);` |
| `excluding-missing-members-other-type` | Manual | `a.Should().BeEquivalentTo(b, o => o.ExcludingMissingMembers());` — reden: ExcludingMissingMembers() on different compile-time types |
| `excluding-two` | Manual | `a.Should().BeEquivalentTo(b, o => o.Excluding(x => x.Id).Excluding(x => x.Name));` — reden: Excluding(x => x.Name): ShouldBeEquivalentTo compares every member |
| `excluding-with-strict` | Manual | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering().Excluding(x => x.Name));` — reden: Excluding(x => x.Name): ShouldBeEquivalentTo compares every member |
| `including` | Manual | `a.Should().BeEquivalentTo(b, o => o.Including(x => x.Id));` — reden: Including(x => x.Id): it changes which members are compared |
| `lenient-collection-ints` | Fixed | `a.Should().BeEquivalentTo(b, o => o.ExcludingMissingMembers());` → `a.ShouldBe(b, ignoreOrder: true);` |
| `options-variable` | Manual | `a.Should().BeEquivalentTo(b, configure);` — reden: The options are no inline lambda |
| `respecting-runtime-types` | Manual | `a.Should().BeEquivalentTo(b, o => o.RespectingRuntimeTypes());` — reden: it picks declared or runtime types |
| `strict-ordering` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` → `a.ShouldBe(b);` |
| `strict-ordering-because` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering(), "order matters");` → `a.ShouldBe(b, ignoreOrder: false, "order matters");` |
| `strict-ordering-because-args` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering(), "order {0}", 1);` → `a.ShouldBe(b, ignoreOrder: false, string.Format(System.Globalization.CultureInfo.InvariantCulture, "order {0}"…` |
| `strict-ordering-class` | Manual | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` — reden: WithStrictOrdering() on a collection of Dto: FluentAssertions compares the items by their members, Shouldly's ShouldBe by Equals |
| `strict-ordering-item-types` | Manual | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` — reden: WithStrictOrdering() on collections with different item types |
| `strict-ordering-lambda-parenthesized` | Fixed | `a.Should().BeEquivalentTo(b, (options) => options.WithStrictOrdering());` → `a.ShouldBe(b);` |
| `strict-ordering-object` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` → `a.ShouldBeEquivalentTo(b);` |
| `strict-ordering-record` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` → `a.ShouldBe(b);` |
| `strict-ordering-record-computed` | Manual | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` — reden: WithStrictOrdering() on a collection of Item |
| `strict-ordering-record-list-member` | Manual | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` — reden: WithStrictOrdering() on a collection of Item |
| `strict-ordering-record-struct` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` → `a.ShouldBe(b);` |
| `strict-ordering-record-unsealed` | Manual | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` — reden: WithStrictOrdering() on a collection of Item |
| `strict-ordering-strings` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` → `a.ShouldBe(b);` |
| `strict-ordering-struct` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());` → `a.ShouldBe(b);` |
| `strict-ordering-with-missing-members` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering().ExcludingMissingMembers());` → `a.ShouldBe(b);` |
| `strict-then-without` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering().WithoutStrictOrdering());` → `a.ShouldBe(b, ignoreOrder: true);` |
| `throwing-on-missing-members` | Fixed | `a.Should().BeEquivalentTo(b, o => o.ThrowingOnMissingMembers());` → `a.ShouldBeEquivalentTo(b);` |
| `using-comparer` | Manual | `a.Should().BeEquivalentTo(b, o => o.Using<decimal>(c => c.Subject.Should().BeApproximately(c.Expectation, 0.01…` — reden: it changes how values are compared |
| `with-tracing` | Manual | `a.Should().BeEquivalentTo(b, o => o.WithTracing());` — reden: it only adds a trace to the failure message |
| `without-strict-ordering` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithoutStrictOrdering());` → `a.ShouldBe(b, ignoreOrder: true);` |
| `without-then-strict` | Fixed | `a.Should().BeEquivalentTo(b, o => o.WithoutStrictOrdering().WithStrictOrdering());` → `a.ShouldBe(b);` |

### HaveCount (19)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `array` | Fixed | `x.Should().HaveCount(3);` → `x.Length.ShouldBe(3);` |
| `because-args` | Fixed | `x.Should().HaveCount(2, "x {0}", y);` → `x.Count.ShouldBe(2, string.Format(System.Globalization.CultureInfo.InvariantCulture, "x {0}", y));` |
| `chain` | Manual | `x.Should().HaveCount(2).And.Contain(1);` — reden: is used |
| `collection-interfaces` | Fixed | `a.Should().HaveCount(1); b.Should().HaveCount(2);` → `a.Count.ShouldBe(1); b.Count.ShouldBe(2);` |
| `count-comparison-because-args` | Fixed | `a.Should().HaveCountGreaterThan(1, "id {0}", 1);` → `a.Count.ShouldBeGreaterThan(1, string.Format(System.Globalization.CultureInfo.InvariantCulture, "id {0}", 1));` |
| `count-comparisons` | Fixed | `a.Should().HaveCountGreaterThan(1); b.Should().HaveCountGreaterThanOrEqualTo(2, "at least two");` → `a.Count.ShouldBeGreaterThan(1); b.Length.ShouldBeGreaterThanOrEqualTo(2, "at least two");` |
| `dictionary` | Manual | `x.Should().HaveCount(2);` — reden: Dictionary: |
| `edge-generic-element` | Fixed | `items.Should().HaveCount(1);` → `items.Count.ShouldBe(1);` |
| `edge-linq-subject` | Fixed | `items.Where(x => x > 0).Should().HaveCount(2);` → `items.Where(x => x > 0).Count().ShouldBe(2);` |
| `edge-multiline` | Fixed | `items .Should()` → `items.Length.ShouldBe(3);` |
| `enumerable` | Fixed | `x.Should().HaveCount(2); Numbers().Should().HaveCount(1);` → `x.Count().ShouldBe(2); Numbers().Count().ShouldBe(1);` |
| `enumerable-because` | Fixed | `x.Should().HaveCount(2, "two items expected");` → `x.Count().ShouldBe(2, "two items expected");` |
| `enumerable-existing-using` | Fixed | `x.Should().HaveCount(2);` → `x.Count().ShouldBe(2);` |
| `explicit-count` | Fixed | `x.Should().HaveCount(1);` → `x.Count().ShouldBe(1);` |
| `list` | Fixed | `x.Should().HaveCount(2);` → `x.Count.ShouldBe(2);` |
| `list-because` | Fixed | `x.Should().HaveCount(2, "two items expected");` → `x.Count.ShouldBe(2, "two items expected");` |
| `nullable` | Fixed | `x.Should().HaveCount(2);` → `x.ShouldNotBeNull().Count.ShouldBe(2);` |
| `predicate` | Manual | `x.Should().HaveCount(c => c > 1);` — reden: Predicate: |
| `receiver-parens` | Fixed | `(a ?? b).Should().HaveCount(1);` → `(a ?? b).Count.ShouldBe(1);` |

### HaveLength (7)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `s.Should().HaveLength(5);` — reden: AssertionScope |
| `because` | Fixed | `s.Should().HaveLength(5, "a code");` → `s.Length.ShouldBe(5, "a code");` |
| `because-args` | Fixed | `s.Should().HaveLength(5, "of {0}", 5);` → `s.Length.ShouldBe(5, string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 5));` |
| `nullable` | Fixed | `s.Should().HaveLength(5);` → `s.ShouldNotBeNull().Length.ShouldBe(5);` |
| `receiver-parens` | Fixed | `(a + b).Should().HaveLength(5);` → `(a + b).Length.ShouldBe(5);` |
| `simple` | Fixed | `s.Should().HaveLength(5);` → `s.Length.ShouldBe(5);` |
| `stream` | Manual | `s.Should().HaveLength(5);` — reden: stream |

### Invoking (20)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `aggregate` | Manual | `order.Invoking(o => o.CancelAsync().Wait()).Should().Throw<InvalidOperationException>();` — reden: AggregateException |
| `aggregate-parameter-and` | Manual | `task.Invoking(_ => _.Wait(TimeSpan.FromSeconds(3))).Should().Throw<Exception>().And.Should().Be(expected);` — reden: the unwrapped inner exception |
| `aggregate-parameter-message` | Manual | `future.Invoking(f => f.Wait()).Should().Throw<Exception>().WithMessage("Stream failed.");` — reden: the unwrapped inner exception |
| `assertion-scope` | Manual | `order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>();` — reden: AssertionScope |
| `awaiting` | Fixed | `await order.Awaiting(o => o.CancelAsync()).Should().ThrowAsync<InvalidOperationException>();` → `await Should.ThrowAsync<InvalidOperationException>(() => order.CancelAsync());` |
| `awaiting-exactly` | Fixed | `await order.Awaiting(o => o.CancelAsync()).Should().ThrowExactlyAsync<InvalidOperationException>();` → `(await Should.ThrowAsync<InvalidOperationException>(() => order.CancelAsync())).ShouldBeOfType<InvalidOperatio…` |
| `awaiting-wait-async` | Manual | `await task.Awaiting(t => t.WaitAsync(TimeSpan.FromSeconds(3))).Should().ThrowAsync<InvalidOperationException>(…` — reden: waits on a task made elsewhere |
| `because-args` | Fixed | `order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>("a {0}", 1);` → `Should.Throw<InvalidOperationException>(() => order.Cancel(), string.Format(System.Globalization.CultureInfo.I…` |
| `because-message` | Fixed | `order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>("it is shipped").WithMessage("*shipp…` → `Should.Throw<InvalidOperationException>(() => order.Cancel(), "it is shipped").Message.ShouldEndWith("shipped"…` |
| `fluent-actions` | Fixed | `FluentActions.Invoking(() => order.Cancel()).Should().Throw<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(() => order.Cancel());` |
| `func` | Fixed | `order.Invoking(o => o.Name()).Should().Throw<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(() => order.Name());` |
| `method-group` | Fixed | `order.Invoking(Run).Should().Throw<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(() => Run(order));` |
| `not-throw` | Fixed | `order.Invoking(o => o.Cancel()).Should().NotThrow();` → `Should.NotThrow(() => order.Cancel());` |
| `result-and` | Fixed | `order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>() .And.Message.Should().Be("Order sh…` → `Should.Throw<InvalidOperationException>(() => order.Cancel()).Message.Should().Be("Order shipped");` |
| `result-delegate-local` | Fixed | `var ex = await order.Awaiting(action).Should().ThrowAsync<InvalidOperationException>(); ex.And.Message.Should(…` → `var ex = await Should.ThrowAsync<InvalidOperationException>(() => action(order)); ex.Message.Should().Be("Orde…` |
| `side-effect-receiver` | Manual | `Create().Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>();` — reden: side effects |
| `simple` | Fixed | `order.Invoking(o => o.Cancel()).Should().Throw<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(() => order.Cancel());` |
| `throw-exactly` | Fixed | `order.Invoking(o => o.Cancel()).Should().ThrowExactly<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(() => order.Cancel()).ShouldBeOfType<InvalidOperationException>();` |
| `value-result` | Fixed | `breaker.Invoking(b => b.Raise()).Should().Throw<InvalidOperationException>(); await breaker.Awaiting(b => b.Ra…` → `Should.Throw<InvalidOperationException>(() => breaker.Raise()); await Should.ThrowAsync<InvalidOperationExcept…` |
| `value-type` | Manual | `date.Invoking(d => d.AddYears(100000)).Should().Throw<ArgumentOutOfRangeException>();` — reden: value type |

### Match (10)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `because` | Fixed | `s.Should().MatchRegex("^SO", "ids start with SO");` → `s.ShouldMatch("^SO", "ids start with SO");` |
| `empty` | Manual | `s.Should().MatchRegex("");` — reden: non-empty string constant |
| `not-regex` | Fixed | `s.Should().NotMatchRegex("x");` → `s.ShouldNotMatch("x");` |
| `not-wildcard` | Fixed | `s.Should().NotMatch("*x*");` → `s.ShouldNotMatch("(?s)\\A.*x.*$");` |
| `nullable` | Fixed | `s.Should().MatchRegex("a");` → `s.ShouldNotBeNull().ShouldMatch("a");` |
| `predicate` | Manual | `o.Should().Match(x => x != null);` — reden: non-string |
| `regex` | Fixed | `s.Should().MatchRegex("^SO-[0-9]+$");` → `s.ShouldMatch("^SO-[0-9]+$");` |
| `regex-object` | Manual | `s.Should().MatchRegex(new Regex("a"));` — reden: non-empty string constant |
| `variable` | Manual | `s.Should().Match(p);` — reden: non-empty string constant |
| `wildcard` | Fixed | `s.Should().Match("SO-*.?");` → `s.ShouldMatch("(?s)\\ASO-.*\\..$");` |

### NotBe (9)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `x.Should().NotBe(1);` — reden: Inside an AssertionScope |
| `because` | Fixed | `status.Should().NotBe(Status.Shipped, "a new order is not shipped yet");` → `status.ShouldNotBe(Status.Shipped, "a new order is not shipped yet");` |
| `because-args` | Fixed | `total.Should().NotBe(0, "{0} items", items);` → `total.ShouldNotBe(0, string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} items", items));` |
| `chain` | Manual | `result.Should().NotBe(41).And.BePositive();` — reden: is used |
| `comparer` | Manual | `actual.Should().NotBe(other, EqualityComparer<object>.Default);` — reden: comparer |
| `constant-cast` | Fixed | `count.Should().NotBe(0);` → `count.ShouldNotBe((byte)0);` |
| `nullable` | Fixed | `count.Should().NotBe(0); name.Should().NotBe("timme");` → `count.ShouldNotBe(0); name.ShouldNotBe("timme");` |
| `record` | Fixed | `price.Should().NotBe(new Money(0m, "EUR"));` → `price.ShouldNotBe(new Money(0m, "EUR"));` |
| `simple` | Fixed | `result.Should().NotBe(41); name.Should().NotBe("timme");` → `result.ShouldNotBe(41); name.ShouldNotBe("timme");` |

### NotContainNulls (21)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `all-be-assertion-scope` | Manual | `x.Should().AllBe("EUR");` — reden: AssertionScope |
| `all-be-because` | Fixed | `x.Should().AllBe("EUR", "uniform");` → `x.ShouldAllBe(i => i == "EUR", "uniform");` |
| `all-be-because-args` | Fixed | `x.Should().AllBe("EUR", "of {0}", 2);` → `x.ShouldAllBe(i => i == "EUR", string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", 2));` |
| `all-be-computed` | Manual | `x.Should().AllBe(x[0] + "!");` — reden: computed |
| `all-be-constant-expression` | Fixed | `x.Should().AllBe("E" + "UR");` → `x.ShouldAllBe(i => i == ("E" + "UR"));` |
| `all-be-member-expected` | Fixed | `x.Should().AllBe(currency);` → `x.ShouldAllBe(i => i == currency);` |
| `all-be-name-conflict` | Fixed | `x.Should().AllBe(i);` → `x.ShouldAllBe(item => item == i);` |
| `all-be-nullable-string` | Fixed | `x.Should().AllBe("EUR");` → `x.ShouldNotBeNull().ShouldAllBe(i => i == "EUR");` |
| `all-be-result-used` | Manual | `var c = x.Should().AllBe("EUR").And;` — reden: is used |
| `all-be-string` | Fixed | `x.Should().AllBe("EUR");` → `x.ShouldAllBe(i => i == "EUR");` |
| `array` | Fixed | `x.Should().NotContainNulls();` → `x.ShouldNotContain(i => i == null);` |
| `assertion-scope` | Manual | `x.Should().NotContainNulls();` — reden: AssertionScope |
| `because` | Fixed | `x.Should().NotContainNulls("no gaps");` → `x.ShouldNotContain(i => i == null, "no gaps");` |
| `because-args` | Fixed | `x.Should().NotContainNulls("of {0}", 2);` → `x.ShouldNotContain(i => i == null, string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", …` |
| `name-conflict` | Fixed | `x.Should().NotContainNulls();` → `x.ShouldNotContain(item => item == null);` |
| `nullable` | Fixed | `x.Should().NotContainNulls();` → `x.ShouldNotBeNull().ShouldNotContain(i => i == null);` |
| `nullable-int` | Fixed | `x.Should().NotContainNulls();` → `x.ShouldNotContain(i => i == null);` |
| `result-used` | Manual | `var c = x.Should().NotContainNulls().And;` — reden: is used |
| `selector` | Manual | `x.Should().NotContainNulls(i => i.Name);` — reden: checks a property |
| `simple` | Fixed | `x.Should().NotContainNulls();` → `x.ShouldNotContain(i => i == null);` |
| `value-type` | Manual | `x.Should().NotContainNulls();` — reden: non-nullable value type |

### NotEqual (12)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `array-literal` | Fixed | `x.Should().NotEqual(new[] { 1, 2 });` → `x.ShouldNotBeNull().ShouldNotBe<System.Collections.Generic.IEnumerable<int>>(new[] { 1, 2 });` |
| `assertion-scope` | Manual | `x.Should().NotEqual(y);` — reden: AssertionScope |
| `because` | Fixed | `x.Should().NotEqual(y, "order matters");` → `x.ShouldNotBeNull().ShouldNotBe(y, "order matters");` |
| `because-args` | Fixed | `x.Should().NotEqual(y, "of {0}", 2);` → `x.ShouldNotBeNull().ShouldNotBe(y, string.Format(System.Globalization.CultureInfo.InvariantCulture, "of {0}", …` |
| `dictionary` | Manual | `x.Should().NotEqual(y);` — reden: dictionary |
| `different-type` | Fixed | `x.Should().NotEqual(y);` → `x.ShouldNotBeNull().ShouldNotBe<System.Collections.Generic.IEnumerable<int>>(y);` |
| `interface-type` | Fixed | `x.Should().NotEqual(y);` → `x.ShouldNotBeNull().ShouldNotBe<System.Collections.Generic.IEnumerable<string>>(y);` |
| `non-null` | Fixed | `x.Should().NotEqual(y);` → `x.ShouldNotBe(y);` |
| `nullable` | Fixed | `x.Should().NotEqual(y);` → `x.ShouldNotBeNull().ShouldNotBe(y);` |
| `result-used` | Manual | `var c = x.Should().NotEqual(y).And;` — reden: is used |
| `same-type` | Fixed | `x.Should().NotEqual(y);` → `x.ShouldNotBeNull().ShouldNotBe(y);` |
| `string-collection` | Fixed | `x.Should().NotEqual(y);` → `x.ShouldNotBeNull().ShouldNotBe(y);` |

### NotThrow (11)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `after` | Manual | `act.Should().NotThrowAfter(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(10));` — reden: NotThrowAfter |
| `assertion-scope` | Manual | `act.Should().NotThrow();` — reden: Inside an AssertionScope |
| `because` | Fixed | `act.Should().NotThrow("it is valid");` → `Should.NotThrow(act, "it is valid");` |
| `because-args` | Fixed | `act.Should().NotThrow("because {0}", 1);` → `Should.NotThrow(act, string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", 1));` |
| `edge-expression-bodied` | Fixed | `public void Test(Action act) => act.Should().NotThrow();` → `public void Test(Action act) => Should.NotThrow(act);` |
| `func` | Fixed | `func.Should().NotThrow();` → `Should.NotThrow(func);` |
| `generic` | Manual | `act.Should().NotThrow<InvalidOperationException>();` — reden: only forbids exception type |
| `result-used` | Manual | `var result = act.Should().NotThrow();` — reden: is used |
| `simple` | Fixed | `act.Should().NotThrow();` → `Should.NotThrow(act);` |
| `value-func` | Fixed | `func.Should().NotThrow();` → `Should.NotThrow(func);` |
| `value-used` | Manual | `var value = func.Should().NotThrow().Which;` — reden: is used |

### NotThrowAsync (16)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `await act.Should().NotThrowAsync();` — reden: AssertionScope |
| `awaiting` | Fixed | `await FluentActions.Awaiting(act).Should().NotThrowAsync();` → `await Should.NotThrowAsync(act);` |
| `awaiting-receiver` | Fixed | `await breaker.Awaiting(x => x.ExecuteAsync(() => Task.CompletedTask)).Should().NotThrowAsync();` → `await Should.NotThrowAsync(() => breaker.ExecuteAsync(() => Task.CompletedTask));` |
| `awaiting-side-effect` | Manual | `await Create().Awaiting(x => x.ExecuteAsync(() => Task.CompletedTask)).Should().NotThrowAsync();` — reden: side effects |
| `awaiting-value-result` | Fixed | `await counter.Awaiting(c => c.CountAsync()).Should().NotThrowAsync();` → `await Should.NotThrowAsync(() => counter.CountAsync());` |
| `because` | Fixed | `await act.Should().NotThrowAsync("must work");` → `await Should.NotThrowAsync(act, "must work");` |
| `because-args` | Fixed | `await act.Should().NotThrowAsync("id {0}", 1);` → `await Should.NotThrowAsync(act, string.Format(System.Globalization.CultureInfo.InvariantCulture, "id {0}", 1))…` |
| `configure-await` | Fixed | `await act.Should().NotThrowAsync().ConfigureAwait(false);` → `await Should.NotThrowAsync(act).ConfigureAwait(false);` |
| `fluent-actions-lambda` | Fixed | `await FluentActions.Awaiting(() => breaker.ExecuteAsync(() => Task.CompletedTask)).Should().NotThrowAsync("it …` → `await Should.NotThrowAsync(() => breaker.ExecuteAsync(() => Task.CompletedTask), "it is closed");` |
| `generic` | Manual | `await act.Should().NotThrowAsync<InvalidOperationException>();` — reden: type T |
| `lambda` | Fixed | `await wrapped.Should().NotThrowAsync();` → `await Should.NotThrowAsync(wrapped);` |
| `not-awaited` | Manual | `Task t = act.Should().NotThrowAsync();` — reden: not awaited as a statement |
| `reference-result` | Fixed | `await act.Should().NotThrowAsync();` → `await Should.NotThrowAsync(act);` |
| `result-used` | Manual | `var r = await act.Should().NotThrowAsync();` — reden: not awaited as a statement |
| `simple` | Fixed | `await act.Should().NotThrowAsync();` → `await Should.NotThrowAsync(act);` |
| `value-result` | Manual | `await act.Should().NotThrowAsync();` — reden: Func<Task> |

### OnlyContain (15)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and-chain` | Manual | `x.Should().HaveCount(2).And.OnlyContain(i => i > 0);` — reden: is not in the form |
| `array` | Fixed | `x.Should().OnlyContain(i => i > 0);` → `x.ShouldNotBeEmpty(); x.ShouldAllBe(i => i > 0);` |
| `because` | Fixed | `x.Should().OnlyContain(i => i > 0, "all are positive");` → `x.ShouldNotBeEmpty("all are positive"); x.ShouldAllBe(i => i > 0, "all are positive");` |
| `because-args` | Fixed | `x.Should().OnlyContain(i => i > 0, "because {0}", "reason");` → `x.ShouldNotBeEmpty(string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", "reason"));…` |
| `chain` | Manual | `x.Should().OnlyContain(i => i > 0).And.HaveCount(2);` — reden: is used |
| `dictionary` | Fixed | `x.Should().OnlyContain(p => p.Value > 0);` → `x.ShouldNotBeEmpty(); x.ShouldAllBe(p => p.Value > 0);` |
| `edge-multiline-lambda` | Fixed | `names.Should().OnlyContain(n =>` → `names.ShouldNotBeEmpty(); names.ShouldAllBe(n =>` |
| `enumerable` | Fixed | `x.Should().OnlyContain(s => s.Length > 1);` → `x.ShouldNotBeEmpty(); x.ShouldAllBe(s => s.Length > 1);` |
| `lambda` | Manual | `Action act = () => x.Should().OnlyContain(i => i > 0);` — reden: cannot hold two statements |
| `member-subject` | Fixed | `other.Items.Should().OnlyContain(i => i > 0); this.Items.Should().OnlyContain(i => i > 0);` → `other.Items.ShouldNotBeEmpty(); other.Items.ShouldAllBe(i => i > 0);` |
| `no-block` | Manual | `x.Should().OnlyContain(i => i > 0);` — reden: cannot hold two statements |
| `nullable` | Fixed | `x.Should().OnlyContain(i => i > 0);` → `x.ShouldNotBeEmpty(); x.ShouldAllBe(i => i > 0);` |
| `simple` | Fixed | `x.Should().OnlyContain(i => i > 0);` → `x.ShouldNotBeEmpty(); x.ShouldAllBe(i => i > 0);` |
| `subject-call` | Fixed | `Load().Should().OnlyContain(i => i > y);` → `var subject = Load(); subject.ShouldNotBeEmpty();` |
| `used` | Manual | `var r = x.Should().OnlyContain(i => i > 0);` — reden: is used |

### Order (15)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope` | Manual | `x.Should().BeInAscendingOrder();` — reden: AssertionScope |
| `because` | Fixed | `x.Should().BeInAscendingOrder(p => p.Qty, "sorted by quantity");` → `x.Select(p => p.Qty).ShouldBeInOrder(SortDirection.Ascending, null, "sorted by quantity");` |
| `because-args` | Fixed | `x.Should().BeInAscendingOrder(p => p.Qty, "sorted by {0}", "quantity");` → `x.Select(p => p.Qty).ShouldBeInOrder(SortDirection.Ascending, null, string.Format(System.Globalization.Culture…` |
| `comparer` | Manual | `x.Should().BeInAscendingOrder(Comparer<int>.Default);` — reden: comparer or comparison |
| `comparison` | Manual | `x.Should().BeInAscendingOrder((a, b) => a.CompareTo(b));` — reden: comparer or comparison |
| `expression-variable` | Manual | `x.Should().BeInAscendingOrder(key);` — reden: Expression variable |
| `no-selector` | Fixed | `x.Should().BeInAscendingOrder(); x.Should().BeInDescendingOrder();` → `x.ShouldBeInOrder(SortDirection.Ascending); x.ShouldBeInOrder(SortDirection.Descending);` |
| `nullable` | Fixed | `x.Should().BeInAscendingOrder(p => p.Qty);` → `x.ShouldNotBeNull().Select(p => p.Qty).ShouldBeInOrder(SortDirection.Ascending);` |
| `result-used` | Manual | `var c = x.Should().BeInAscendingOrder().And;` — reden: is used |
| `selector-ascending` | Fixed | `x.Should().BeInAscendingOrder(p => p.Qty);` → `x.Select(p => p.Qty).ShouldBeInOrder(SortDirection.Ascending);` |
| `selector-comparer` | Manual | `x.Should().BeInAscendingOrder(p => p.Sku, StringComparer.OrdinalIgnoreCase);` — reden: comparer or comparison |
| `selector-descending` | Fixed | `x.Should().BeInDescendingOrder(p => p.Qty);` → `x.Select(p => p.Qty).ShouldBeInOrder(SortDirection.Descending);` |
| `string-because` | Fixed | `x.Should().BeInAscendingOrder("names");` → `x.ShouldBeInOrder(SortDirection.Ascending, System.StringComparer.Ordinal, "names");` |
| `string-elements` | Fixed | `x.Should().BeInDescendingOrder();` → `x.ShouldBeInOrder(SortDirection.Descending, System.StringComparer.Ordinal);` |
| `string-key` | Fixed | `x.Should().BeInAscendingOrder(p => p.Sku);` → `x.Select(p => p.Sku).ShouldBeInOrder(SortDirection.Ascending, System.StringComparer.Ordinal);` |

### ShouldBe (34)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `assertion-scope-nested` | Manual | `x.Should().Be(1);` — reden: Inside an AssertionScope |
| `assertion-scope-using-block` | Manual | `x.Should().Be(1);` — reden: Inside an AssertionScope |
| `assertion-scope-using-var` | Manual | `x.Should().Be(1);` — reden: Inside an AssertionScope |
| `because` | Fixed | `total.Should().Be(25, "two items of 12.50");` → `total.ShouldBe(25, "two items of 12.50");` |
| `because-args` | Fixed | `total.Should().Be(25, "{0} items", items);` → `total.ShouldBe(25, string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} items", items));` |
| `because-args-bad-format` | Manual | `total.Should().Be(25, "{1} items", items);` — reden: becauseArgs |
| `because-args-variable` | Manual | `total.Should().Be(25, why, items);` — reden: becauseArgs |
| `chain` | Manual | `result.Should().Be(42).And.NotBe(41);` — reden: is used |
| `comparer` | Manual | `actual.Should().Be(expected, EqualityComparer<object>.Default);` — reden: comparer |
| `constant-cast` | Fixed | `count.Should().Be(1); status.Should().Be(0);` → `count.ShouldBe((byte)1); status.ShouldBe((Status)0);` |
| `edge-assertion-in-throw-lambda` | Manual | `Action act = () => total.Should().Be(3);` — reden: exception type |
| `edge-await-subject` | Fixed | `(await load()).Should().Be(1);` → `(await load()).ShouldBe(1);` |
| `edge-cast-subject` | Fixed | `((string)value).Should().Be("a");` → `((string)value).ShouldBe("a");` |
| `edge-enum` | Fixed | `day.Should().Be(DayOfWeek.Monday);` → `day.ShouldBe(DayOfWeek.Monday);` |
| `edge-expression-bodied` | Fixed | `public void Test(int value) => value.Should().Be(1);` → `public void Test(int value) => value.ShouldBe(1);` |
| `edge-generic` | Fixed | `value.Should().Be(expected);` → `value.ShouldBe(expected);` |
| `edge-global-using` | Fixed | `global using FluentAssertions; value.Should().Be(1);` → `global using Shouldly; value.ShouldBe(1);` |
| `edge-ifdef` | Fixed | `value.Should().Be(2);` → `value.ShouldBe(2);` |
| `edge-interpolated-because` | Fixed | `total.Should().Be(3, $"{count} items were added");` → `total.ShouldBe(3, $"{count} items were added");` |
| `edge-interpolated-expected` | Fixed | `url.Should().Be($"{root}/license");` → `url.ShouldBe($"{root}/license");` |
| `edge-method-chain-subject` | Fixed | `text.Trim().ToUpperInvariant().Should().Be("A");` → `text.Trim().ToUpperInvariant().ShouldBe("A");` |
| `edge-multiline` | Fixed | `value .Should()` → `value.ShouldBe(42);` |
| `edge-named-because` | Manual | `total.Should().Be(3, because: "three items were added");` — reden: named arguments |
| `edge-raw-string` | Fixed | `json.Should().Be("""{"name":"a"}""");` → `json.ShouldBe("""{"name":"a"}""");` |
| `edge-trailing-comment` | Fixed | `value.Should().Be(42); // keep me` → `value.ShouldBe(42); // keep me` |
| `edge-using-after-file-scoped-namespace` | Fixed | `result.Should().Be(42);` → `result.ShouldBe(42);` |
| `edge-using-in-namespace` | Fixed | `result.Should().Be(42);` → `result.ShouldBe(42);` |
| `implicit-conversion` | Fixed | `doc.Should().Be(42); doc.Should().Be("John", "name");` → `doc.ShouldBe((Doc)42); doc.ShouldBe((Doc)"John", "name");` |
| `lambda` | Fixed | `Array.ForEach(totals, t => t.Should().Be(20m));` → `Array.ForEach(totals, t => t.ShouldBe(20m));` |
| `nullable-expected` | Manual | `count.Should().Be(expected);` — reden: differs from subject type |
| `own-equals` | Fixed | `actual.Should().Be(expected);` → `actual.ShouldBe(expected);` |
| `own-shouldbe-in-namespace` | Manual | `total.Should().Be(25, "{0} items", items);` — reden: binds to Proofs.TestKit.SpecExtensions.ShouldBe instead of Shouldly |
| `simple` | Fixed | `result.Should().Be(42); name.Should().Be("timme");` → `result.ShouldBe(42); name.ShouldBe("timme");` |
| `xml` | Manual | `actual.Should().Be(expected);` — reden: different equality semantics |

### StringStartEnd (10)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and` | Manual | `s.Should().StartWith("Ti").And.EndWith("me");` — reden: .And |
| `assertion-scope` | Manual | `s.Should().StartWith("Ti");` — reden: AssertionScope |
| `because` | Fixed | `s.Should().StartWith("Ti", "name"); s.Should().NotEndWith("x", "name");` → `s.ShouldStartWith("Ti", Shouldly.Case.Sensitive, "name"); s.ShouldNotEndWith("x", "name", Shouldly.Case.Sensit…` |
| `because-args` | Fixed | `s.Should().StartWith("Ti", "of {0}", 1);` → `s.ShouldStartWith("Ti", Shouldly.Case.Sensitive, string.Format(System.Globalization.CultureInfo.InvariantCultu…` |
| `collection` | Manual | `x.Should().StartWith(1);` — reden: non-string |
| `edge-member-subject` | Fixed | `dto.Name.Should().StartWith($"{dto.Name[0]}");` → `dto.Name.ShouldStartWith($"{dto.Name[0]}", Shouldly.Case.Sensitive);` |
| `equivalent-of` | Fixed | `s.Should().StartWithEquivalentOf("ti"); s.Should().EndWithEquivalentOf("ME");` → `s.ShouldStartWith("ti"); s.ShouldEndWith("ME");` |
| `null-or` | Fixed | `s.Should().NotBeNullOrEmpty(); s.Should().NotBeNullOrWhiteSpace();` → `s.ShouldNotBeNullOrEmpty(); s.ShouldNotBeNullOrWhiteSpace();` |
| `nullable` | Fixed | `s.Should().StartWith("Ti"); s.Should().NotBeNullOrWhiteSpace();` → `s.ShouldStartWith("Ti", Shouldly.Case.Sensitive); s.ShouldNotBeNullOrWhiteSpace();` |
| `simple` | Fixed | `s.Should().StartWith("Ti"); s.Should().EndWith("me");` → `s.ShouldStartWith("Ti", Shouldly.Case.Sensitive); s.ShouldEndWith("me", Shouldly.Case.Sensitive);` |

### Throw (46)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `aggregate-exception-and` | Manual | `action.Should().Throw<Exception>().And.Should().BeSameAs(expected);` — reden: the unwrapped inner exception |
| `aggregate-exception-plain` | Fixed | `action.Should().Throw<Exception>();` → `Should.Throw<Exception>(action);` |
| `aggregate-expected` | Fixed | `act.Should().Throw<AggregateException>();` → `Should.Throw<AggregateException>(act);` |
| `aggregate-get-result` | Fixed | `act.Should().Throw<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(act);` |
| `aggregate-helper-method` | Manual | `fail.Should().Throw<InvalidOperationException>();` — reden: unwraps AggregateException |
| `aggregate-method-group` | Manual | `act.Should().Throw<InvalidOperationException>();` — reden: unwraps AggregateException |
| `aggregate-wait` | Manual | `act.Should().Throw<InvalidOperationException>();` — reden: unwraps AggregateException |
| `assertion-helper` | Manual | `candidate.Should().Throw<AssertionFailedException>();` — reden: Throw<XunitException> |
| `because` | Fixed | `act.Should().Throw<InvalidOperationException>("it is shipped");` → `Should.Throw<InvalidOperationException>(act, "it is shipped");` |
| `because-args` | Fixed | `act.Should().Throw<InvalidOperationException>("because {0}", 1);` → `Should.Throw<InvalidOperationException>(act, string.Format(System.Globalization.CultureInfo.InvariantCulture, …` |
| `chain` | Fixed | `act.Should().Throw<InvalidOperationException>().WithInnerException<ArgumentException>();` → `Should.Throw<InvalidOperationException>(act).InnerException.ShouldNotBeNull().ShouldBeAssignableTo<ArgumentExc…` |
| `edge-assertion-in-throw-lambda` | Manual | `Action act = () => total.Should().Be(3);` — reden: exception type |
| `edge-inner-exception` | Fixed | `act.Should().Throw<InvalidOperationException>().WithInnerException<ArgumentException>();` → `Should.Throw<InvalidOperationException>(act).InnerException.ShouldNotBeNull().ShouldBeAssignableTo<ArgumentExc…` |
| `edge-multiline-message` | Fixed | `act.Should() .Throw<InvalidOperationException>()` → `Should.Throw<InvalidOperationException>(act).Message.ShouldBe("boom", Shouldly.StringCompareShould.IgnoreCase)…` |
| `edge-which-message` | Fixed | `string message = act.Should().Throw<Exception>().Which.Message;` → `string message = Should.Throw<Exception>(act).Message;` |
| `func` | Fixed | `func.Should().Throw<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(func);` |
| `lambda` | Fixed | `Action run = () => act.Should().Throw<InvalidOperationException>();` → `Action run = () => Should.Throw<InvalidOperationException>(act);` |
| `message-because` | Fixed | `act.Should().Throw<InvalidOperationException>("a").WithMessage("*shipped*", "b");` → `Should.Throw<InvalidOperationException>(act, "a").Message.ShouldContain("shipped", Shouldly.Case.Insensitive, …` |
| `message-because-args` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("*a*", "because {0}", 1);` → `Should.Throw<InvalidOperationException>(act).Message.ShouldContain("a", Shouldly.Case.Insensitive, string.Form…` |
| `message-contains` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("*shipped*");` → `Should.Throw<InvalidOperationException>(act).Message.ShouldContain("shipped", Shouldly.Case.Insensitive);` |
| `message-contains-line-break` | Manual | `act.Should().Throw<InvalidOperationException>().WithMessage("*order\nshipped*");` — reden: contains a line break |
| `message-ends` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("*shipped");` → `Should.Throw<InvalidOperationException>(act).Message.ShouldEndWith("shipped", Shouldly.Case.Insensitive);` |
| `message-exact` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("Order shipped");` → `Should.Throw<InvalidOperationException>(act).Message.ShouldBe("Order shipped", Shouldly.StringCompareShould.Ig…` |
| `message-exact-because` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("Order shipped", "b");` → `Should.Throw<InvalidOperationException>(act).Message.ShouldBe("Order shipped", "b", Shouldly.StringCompareShou…` |
| `message-line-break` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("Order\nshipped");` → `Should.Throw<InvalidOperationException>(act).Message.ShouldBe("Order\nshipped", Shouldly.StringCompareShould.I…` |
| `message-starts` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("Order*");` → `Should.Throw<InvalidOperationException>(act).Message.ShouldStartWith("Order", Shouldly.Case.Insensitive);` |
| `message-variable` | Manual | `act.Should().Throw<InvalidOperationException>().WithMessage(pattern);` — reden: not a string constant |
| `message-wildcard` | Manual | `act.Should().Throw<InvalidOperationException>().WithMessage("*ship?ed*");` — reden: wildcards other than |
| `result-and-multiline` | Fixed | `act.Should().Throw<ArgumentOutOfRangeException>() .And.ParamName.Should().Be("maxParallelization");` → `Should.Throw<ArgumentOutOfRangeException>(act).ParamName.Should().Be("maxParallelization");` |
| `result-and-param-name` | Fixed | `act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("maxParallelization");` → `Should.Throw<ArgumentOutOfRangeException>(act).ParamName.Should().Be("maxParallelization");` |
| `result-inner-exactly` | Fixed | `act.Should().Throw<InvalidOperationException>().WithInnerExceptionExactly<ArgumentException>("the cause is kep…` → `Should.Throw<InvalidOperationException>(act).InnerException.ShouldBeOfType<ArgumentException>("the cause is ke…` |
| `result-inner-type` | Manual | `act.Should().Throw<InvalidOperationException>().WithInnerException(typeof(ArgumentException));` — reden: runtime type |
| `result-local` | Fixed | `var ex = act.Should().Throw<ArgumentException>(); ex.And.ParamName.Should().Be("id");` → `var ex = Should.Throw<ArgumentException>(act); ex.ParamName.Should().Be("id");` |
| `result-local-passed` | Manual | `var ex = act.Should().Throw<ArgumentException>();` — reden: used as a value |
| `result-message-and` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("Order*").And.HelpLink.Should().Be("help");` → `var exception = Should.Throw<InvalidOperationException>(act); exception.Message.ShouldStartWith("Order", Shoul…` |
| `result-message-inner` | Fixed | `act.Should().Throw<InvalidOperationException>().WithMessage("Order shipped").WithInnerException<ArgumentExcept…` → `var exception = Should.Throw<InvalidOperationException>(act); exception.Message.ShouldBe("Order shipped", Shou…` |
| `result-param-name` | Fixed | `act.Should().Throw<ArgumentException>().WithParameterName("id");` → `Should.Throw<ArgumentException>(act).ParamName.ShouldBe("id");` |
| `result-passed` | Manual | `Inspect(act.Should().Throw<ArgumentException>());` — reden: used as a value |
| `result-two-locals` | Fixed | `act.Should().Throw<ArgumentException>().WithMessage("*id*").And.ParamName.Should().Be("id"); act.Should().Thro…` → `var exception = Should.Throw<ArgumentException>(act); exception.Message.ShouldContain("id", Shouldly.Case.Inse…` |
| `result-where` | Fixed | `act.Should().Throw<InvalidOperationException>().Where(e => e.Message.Length > 0);` → `var exception = Should.Throw<InvalidOperationException>(act); (exception.Message.Length > 0).ShouldBeTrue();` |
| `result-where-because` | Fixed | `act.Should().Throw<ArgumentException>().Where(e => e.ParamName == "id", "the id is checked");` → `var exception = Should.Throw<ArgumentException>(act); (exception.ParamName == "id").ShouldBeTrue("the id is ch…` |
| `result-where-expression` | Manual | `act.Should().Throw<ArgumentException>().Where(condition);` — reden: one-parameter lambda |
| `result-where-in-lambda` | Manual | `Action check = () => act.Should().Throw<ArgumentException>().Where(e => e.ParamName == "id");` — reden: not a statement in a block |
| `simple` | Fixed | `act.Should().Throw<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(act);` |
| `value-func` | Manual | `func.Should().Throw<InvalidOperationException>();` — reden: reference type |
| `value-used` | Fixed | `var exception = act.Should().Throw<InvalidOperationException>().Which;` → `var exception = Should.Throw<InvalidOperationException>(act);` |

### ThrowAsync (31)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `aggregate-awaited` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>();` → `await Should.ThrowAsync<InvalidOperationException>(act);` |
| `aggregate-result` | Manual | `await act.Should().ThrowAsync<InvalidOperationException>();` — reden: unwraps AggregateException |
| `assertion-scope` | Manual | `await act.Should().ThrowAsync<InvalidOperationException>();` — reden: Inside an AssertionScope |
| `because` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>("a");` → `await Should.ThrowAsync<InvalidOperationException>(act, "a");` |
| `because-args` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>("a {0}", 1);` → `await Should.ThrowAsync<InvalidOperationException>(act, string.Format(System.Globalization.CultureInfo.Invaria…` |
| `because-variable` | Manual | `await act.Should().ThrowAsync<InvalidOperationException>(why, 1);` — reden: becauseArgs whose because is no constant format string |
| `cancelled-inner` | Manual | `(await action.Should().ThrowAsync<TaskCanceledException>()).WithInnerException<TimeoutException>();` — reden: returns a new TaskCanceledException |
| `edge-configure-await` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);` → `await Should.ThrowAsync<InvalidOperationException>(act).ConfigureAwait(false);` |
| `edge-local-async-lambda` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>();` → `await Should.ThrowAsync<InvalidOperationException>(act);` |
| `func-task-reference` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>();` → `await Should.ThrowAsync<InvalidOperationException>(act);` |
| `message-because` | Fixed | `await act.Should().ThrowAsync<ArgumentException>("a").WithMessage("*shipped*", "b");` → `(await Should.ThrowAsync<ArgumentException>(act, "a")).Message.ShouldContain("shipped", Shouldly.Case.Insensit…` |
| `message-because-args` | Fixed | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("*x*", "a {0}", 1);` → `(await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldContain("x", Shouldly.Case.Insensitive, string…` |
| `message-contains` | Fixed | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one line*");` → `(await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldContain("at least one line", Shouldly.Case.Ins…` |
| `message-contains-line-break` | Manual | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least\none line*");` — reden: contains a line break |
| `message-ends` | Fixed | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("*shipped");` → `(await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldEndWith("shipped", Shouldly.Case.Insensitive);` |
| `message-exact` | Fixed | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("Order shipped");` → `(await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldBe("Order shipped", Shouldly.StringCompareShou…` |
| `message-exact-because` | Fixed | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("Order shipped", "b");` → `(await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldBe("Order shipped", "b", Shouldly.StringCompar…` |
| `message-line-break` | Fixed | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("Order\nshipped");` → `(await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldBe("Order\nshipped", Shouldly.StringCompareSho…` |
| `message-starts` | Fixed | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("order*");` → `(await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldStartWith("order", Shouldly.Case.Insensitive);` |
| `message-variable` | Manual | `await act.Should().ThrowAsync<ArgumentException>().WithMessage(pattern);` — reden: not a string constant |
| `message-wildcard` | Manual | `await act.Should().ThrowAsync<ArgumentException>().WithMessage("a*b");` — reden: wildcards other than |
| `named-because` | Manual | `await act.Should().ThrowAsync<InvalidOperationException>(because: "it is invalid");` — reden: named arguments |
| `no-await` | Manual | `var task = act.Should().ThrowAsync<InvalidOperationException>();` — reden: not directly awaited |
| `result-inner-awaited` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>().WithInnerException<InvalidOperationException, Argum…` → `(await Should.ThrowAsync<InvalidOperationException>(act)).InnerException.ShouldNotBeNull().ShouldBeAssignableT…` |
| `result-local-and` | Fixed | `var ex = await act.Should().ThrowAsync<OperationCanceledException>(); ex.And.CancellationToken.Should().Be(tok…` → `var ex = await Should.ThrowAsync<OperationCanceledException>(act); ex.CancellationToken.Should().Be(token);` |
| `result-local-message-inner` | Fixed | `var ex = await act.Should().ThrowAsync<InvalidOperationException>() .WithMessage("The circuit is now open and …` → `var ex = await Should.ThrowAsync<InvalidOperationException>(act); ex.Message.ShouldBe("The circuit is now open…` |
| `result-parenthesized-and` | Fixed | `(await act.Should().ThrowAsync<ArgumentException>()).And.ParamName.Should().Be("id");` → `(await Should.ThrowAsync<ArgumentException>(act)).ParamName.Should().Be("id");` |
| `simple` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>();` → `await Should.ThrowAsync<InvalidOperationException>(act);` |
| `value-func` | Manual | `await act.Should().ThrowAsync<InvalidOperationException>();` — reden: reference type |
| `value-used` | Fixed | `var thrown = await act.Should().ThrowAsync<InvalidOperationException>();` → `var thrown = await Should.ThrowAsync<InvalidOperationException>(act);` |
| `where` | Fixed | `await act.Should().ThrowAsync<InvalidOperationException>().Where(e => e.Message.Length > 0);` → `var exception = await Should.ThrowAsync<InvalidOperationException>(act); (exception.Message.Length > 0).Should…` |

### ThrowExactly (12)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `aggregate-wait` | Fixed | `act.Should().ThrowExactly<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>();` |
| `assertion-scope` | Manual | `act.Should().ThrowExactly<InvalidOperationException>();` — reden: AssertionScope |
| `async` | Fixed | `await act.Should().ThrowExactlyAsync<InvalidOperationException>();` → `(await Should.ThrowAsync<InvalidOperationException>(act)).ShouldBeOfType<InvalidOperationException>();` |
| `async-cancelled` | Manual | `await action.Should().ThrowExactlyAsync<OperationCanceledException>();` — reden: returns a new TaskCanceledException |
| `async-message` | Fixed | `await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Order shipped");` → `(await Should.ThrowAsync<InvalidOperationException>(act)).ShouldBeOfType<InvalidOperationException>().Message.…` |
| `async-task-canceled` | Fixed | `await action.Should().ThrowExactlyAsync<TaskCanceledException>();` → `(await Should.ThrowAsync<TaskCanceledException>(action)).ShouldBeOfType<TaskCanceledException>();` |
| `because` | Fixed | `act.Should().ThrowExactly<InvalidOperationException>("it is shipped");` → `Should.Throw<InvalidOperationException>(act, "it is shipped").ShouldBeOfType<InvalidOperationException>();` |
| `because-args` | Fixed | `act.Should().ThrowExactly<InvalidOperationException>("a {0}", 1);` → `Should.Throw<InvalidOperationException>(act, string.Format(System.Globalization.CultureInfo.InvariantCulture, …` |
| `message` | Fixed | `act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Order*");` → `Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>().Message.ShouldStartWi…` |
| `result-exactly-inner` | Fixed | `act.Should().ThrowExactly<InvalidOperationException>().WithInnerException<ArgumentException>();` → `Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>().InnerException.Should…` |
| `simple` | Fixed | `act.Should().ThrowExactly<InvalidOperationException>();` → `Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>();` |
| `value-used` | Fixed | `string message = act.Should().ThrowExactly<InvalidOperationException>().Which.Message;` → `string message = Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>().Mess…` |

### TimeSpanExtensions (7)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `and` | Manual |  — reden: has no direct replacement |
| `month` | Fixed | `public DateTime Test(int year) => 1.March(year);` → `public DateTime Test(int year) => new DateTime(year, 3, 1);` |
| `month-computed` | Manual |  — reden: with a computed day and year |
| `no-using-system` | Fixed | `public object Test() => 5.Seconds();` → `public object Test() => TimeSpan.FromSeconds(5);` |
| `offset` | Manual |  — reden: has no direct replacement |
| `simple` | Fixed | `Wait(3.Seconds()); Wait(200.Milliseconds());` → `Wait(TimeSpan.FromSeconds(3)); Wait(TimeSpan.FromMilliseconds(200));` |
| `static-call` | Fixed | `public TimeSpan Test() => FluentTimeSpanExtensions.Seconds(5);` → `public TimeSpan Test() => TimeSpan.FromSeconds(5);` |

## MediatR

### _Combined (11)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `add-mediatr-internal-handlers-other-project` | Fixed | `public Task Handle(ContributorDeletedEvent domainEvent, CancellationToken cancellationToken) => Task.Completed…` → `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Clean.Architecture.Web")] public ValueTask Hand…` |
| `autofac-mediator-module` | Fixed | `where TCommand : ICommand public class AddMeetingCommentCommand : ICommand` → `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyMeetings.Modules.Meetings.Infrastructure")] w…` |
| `generic-decorator-marker-without-using` | Fixed | `where TCommand : ICommand public class CreateMeetingCommand : ICommand` → `where TCommand : global::App.ICommand public class CreateMeetingCommand : global::App.ICommand` |
| `generic-decorators-kgrzybek` | Fixed | `public class DomainNotificationBase<T> : IDomainEventNotification<T> where TCommand : ICommand` → `public abstract class DomainNotificationBase<T> : IDomainEventNotification<T> where TCommand : global::Proofs.…` |
| `generic-notification-nadirbad` | Fixed | `public class DomainEventNotification<TDomainEvent> : INotification public Task Handle(DomainEventNotification<…` → `public abstract class DomainEventNotification<TDomainEvent> : INotification public class DomainEventNotificati…` |
| `host-references-host` | Fixed | `public Task Handle(ProcessInboxCommand request, CancellationToken cancellationToken) => Task.CompletedTask; bu…` → `public ValueTask<Unit> Handle(ProcessInboxCommand request, CancellationToken cancellationToken) => Unit.ValueT…` |
| `identified-command-eshop` | Fixed | `public class IdentifiedCommand<T, R> : IRequest<R> public async Task<R> Handle(IdentifiedCommand<T, R> message…` → `public abstract class IdentifiedCommand<T, R> : IRequest<R> }` |
| `internal-message-access` | Fixed | `public Task Handle(CancelMeetingCommand request, CancellationToken cancellationToken) => Task.CompletedTask; b…` → `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyMeetings.Modules.Meetings.Infrastructure")] p…` |
| `request-handler-delegate` | Fixed | `public async Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) ret…` → `public async ValueTask<int> Handle(Register command, MessageHandlerDelegate<Register, int> next, CancellationT…` |
| `request-handler-delegate-shared-field` | Fixed | `public async Task<Response> Handle(TRequest request, RequestHandlerDelegate<Response> next, CancellationToken …` → `public async ValueTask<Response> Handle(TRequest request, MessageHandlerDelegate<TRequest, Response> next, Can…` |
| `request-processor-callers` | Fixed | `public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull public Task …` → `public class LoggingBehaviour<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse> where TRequest :…` |

### AddMediatR (14)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `behavior-lifetime` | Manual | `services.AddMediatR(cfg => cfg.AddOpenBehavior(typeof(LoggingBehavior<,>), ServiceLifetime.Singleton));` — reden: cfg.AddOpenBehavior(...) has no direct Mediator equivalent |
| `behaviors` | Fixed | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ValidationBehavior>();` → `services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Scoped;` |
| `edge-assemblies` | Fixed | `services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(handlers));` → `_ = handlers; services.AddMediator(configuration => configuration.ServiceLifetime = ServiceLifetime.Transient)…` |
| `edge-assemblies-friend` | Fixed | `services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(handlers));` → `_ = handlers; services.AddMediator(configuration => configuration.ServiceLifetime = ServiceLifetime.Transient)…` |
| `edge-assemblies-public` | Manual | `services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(handlers));` — reden: AddMediatR scans the assembly passed in 'handlers' of a public method |
| `edge-publisher-qualified` | Fixed | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);` → `services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient;` |
| `edge-service-type-behavior` | Fixed | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);` → `services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient;` |
| `empty` | Fixed | `services.AddMediatR(cfg => { });` → `services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient);` |
| `expression` | Fixed | `services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(Registration).Assembly));` → `services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Transient);` |
| `generic-handlers` | Fixed | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);` → `services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient;` |
| `no-lambda` | Manual | `services.AddMediatR(configuration);` — reden: AddMediatR is not called as services.AddMediatR(cfg => ...) |
| `processors` | Fixed | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);` → `services.AddMediator(cfg => // logging` |
| `publisher` | Fixed | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);` → `services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient;` |
| `unknown-call` | Manual | `services.AddMediatR(cfg =>` — reden: cfg.AddOpenBehaviors(...) has no direct Mediator equivalent |

### AutofacMediatorModule (3)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `kgrzybek-module` | Fixed | `where TCommand : ICommand builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly)` → `where TCommand : global::MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation.ICommand // Mediat…` |
| `scan-lifetime` | Manual |  — reden: configures more than AsImplementedInterfaces and a lifetime |
| `with-processor` | Manual |  — reden: runs request processors such as LoggingPreProcessor |

### BehaviorDiRegistration (2)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `lifetimes` | Fixed | alleen usings |
| `transient` | Fixed | alleen usings |

### ExceptionHandler (2)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `set-handled` | Fixed | `public class FallbackHandler : IRequestExceptionHandler<Query, int, KeyNotFoundException> public Task Handle(Q…` → `public class FallbackHandler : MessageExceptionHandler<Query, int, KeyNotFoundException> protected override Va…` |
| `state-passed` | Manual | `public Task Handle(Query request, InvalidOperationException exception, RequestExceptionHandlerState<int> state…` — reden: state is used other than as state.SetHandled(x) |

### GenericDecorator (4)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `decorated-members` | Manual | `public Task Handle(T request, CancellationToken cancellationToken) => _decorated.Handle(request, cancellationT…` — reden: ICommandHandler declares members of its own |
| `decorated-other-use` | Manual | `public Task Handle(T request, CancellationToken cancellationToken)` — reden: is used other than to call Handle |
| `notification-without-autofac` | Manual | `public async Task Handle(T notification, CancellationToken cancellationToken)` — reden: registers the open generic DispatchingDecorator<T> as a handler of every notification |
| `used-elsewhere` | Manual | `public Task Handle(T request, CancellationToken cancellationToken) => _decorated.Handle(request, cancellationT…` — reden: is used other than in Autofac's RegisterGenericDecorator |

### GenericNotification (5)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `generic-code` | Manual |  — reden: concrete notification |
| `last-in-file` | Fixed | `public async Task PlaceAsync(IPublisher publisher) => await publisher.Publish(new Placed<int>(1)); public clas…` → `public async Task PlaceAsync(IPublisher publisher) => await publisher.Publish(new PlacedOfInt32(1)); public ab…` |
| `make-generic-type` | Fixed | `public class DomainEventNotification<TDomainEvent> : INotification typeof(DomainEventNotification<>).MakeGener…` → `public abstract class DomainEventNotification<TDomainEvent> : INotification }` |
| `new-closed` | Fixed | `public sealed class Envelope<TEvent> : INotification await _publisher.Publish(new Envelope<OrderPlaced>(new Or…` → `public abstract class Envelope<TEvent> : INotification }` |
| `no-constraint` | Manual |  — reden: no class or interface constraint |

### GenericRequest (5)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `closed-uses` | Fixed | `public class Retried<TRequest, TResponse> : IRequest<TResponse> Retried<Ping, int> retried = new(ping, 3);` → `public abstract class Retried<TRequest, TResponse> : IRequest<TResponse> public class RetriedOfPingAndInt32 : …` |
| `derived-generic-record` | Manual |  — reden: is a record, partial or file-local |
| `not-plain-argument` | Manual |  — reden: is not a plain |
| `open-creation` | Manual |  — reden: is created with open type arguments in generic code |
| `unbound-typeof` | Manual |  — reden: Retried becomes abstract with one concrete subclass per closed type |

### MediatorImplementation (3)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `no-op` | Fixed | `public Task Publish(object notification, CancellationToken cancellationToken = default) {` → `public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken =…` |
| `publisher` | Fixed | `public Task Publish(object notification, CancellationToken cancellationToken = default) {` → `public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = defa…` |
| `with-logic` | Manual |  — reden: does more than complete |

### NotificationHandler (10)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `async` | Fixed | `public async Task Handle(Placed notification, CancellationToken cancellationToken)` → `public async ValueTask Handle(Placed notification, CancellationToken cancellationToken)` |
| `completed-task` | Fixed | `public Task Handle(Placed notification, CancellationToken cancellationToken) return Task.CompletedTask;` → `public ValueTask Handle(Placed notification, CancellationToken cancellationToken) return ValueTask.CompletedTa…` |
| `completed-task-expression` | Fixed | `public Task Handle(Placed notification, CancellationToken cancellationToken) => Task.CompletedTask;` → `public ValueTask Handle(Placed notification, CancellationToken cancellationToken) => ValueTask.CompletedTask;` |
| `edge-marker-interface` | Fixed | `public async Task Handle(OrderPlaced notification, CancellationToken cancellationToken)` → `public async ValueTask Handle(OrderPlaced notification, CancellationToken cancellationToken)` |
| `edge-namespace-shadow` | Fixed | `public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) => Task.CompletedTask;` → `public ValueTask Handle(OrderPlaced notification, CancellationToken cancellationToken) => ValueTask.CompletedT…` |
| `explicit` | Manual | `Task INotificationHandler<Placed>.Handle(Placed notification, CancellationToken cancellationToken) => Task.Com…` — reden: explicit interface |
| `null-return` | Manual | `public Task Handle(Placed notification, CancellationToken cancellationToken) => null!;` — reden: not a Task |
| `other-task` | Fixed | `public Task Handle(Placed notification, CancellationToken cancellationToken) => Task.Delay(notification.Id, ca…` → `public ValueTask Handle(Placed notification, CancellationToken cancellationToken) => new ValueTask(Task.Delay(…` |
| `other-task-block` | Fixed | `public Task Handle(Placed notification, CancellationToken cancellationToken) return Task.CompletedTask;` → `public ValueTask Handle(Placed notification, CancellationToken cancellationToken) return ValueTask.CompletedTa…` |
| `two-handlers` | Fixed | `public Task Handle(Placed notification, CancellationToken cancellationToken) => Task.CompletedTask; public asy…` → `public ValueTask Handle(Placed notification, CancellationToken cancellationToken) => ValueTask.CompletedTask; …` |

### PipelineBehavior (10)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `closed` | Fixed | `public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) return Ta…` → `public ValueTask<int> Handle(Register command, MessageHandlerDelegate<Register, int> next, CancellationToken t…` |
| `edge-notnull-constraint` | Fixed | `where TRequest : notnull public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TRespons…` → `where TRequest : notnull, IMessage public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDe…` |
| `edge-request-constraint` | Fixed | `public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToke…` → `public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, C…` |
| `explicit` | Manual | `Task<TResponse> IPipelineBehavior<TRequest, TResponse>.Handle(TRequest request, RequestHandlerDelegate<TRespon…` — reden: explicit interface implementation |
| `new-constraint` | Fixed | `where TRequest : class, new() public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TRe…` → `where TRequest : class, IMessage, new() public async ValueTask<TResponse> Handle(TRequest request, MessageHand…` |
| `next-passed` | Manual | `public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken canc…` — reden: Handle returns a Task that is not next() |
| `next-stored` | Manual | `public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToke…` — reden: next is used other than as next() |
| `next-token` | Fixed | `where TRequest : notnull public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> nex…` → `where TRequest : notnull, IMessage public ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate…` |
| `no-constraint` | Fixed | `public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken canc…` → `where TRequest : IMessage public ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest…` |
| `open-async` | Fixed | `where TRequest : notnull public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TRespons…` → `where TRequest : notnull, IMessage public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDe…` |

### RequestHandler (16)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `async` | Fixed | `public async Task<int> Handle(Query request, CancellationToken cancellationToken)` → `public async ValueTask<int> Handle(Query request, CancellationToken cancellationToken)` |
| `decorator-inner-handle` | Fixed | `public Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)` → `public ValueTask<TResult> Handle(TRequest request, CancellationToken cancellationToken)` |
| `edge-abstract-generic-base` | Fixed | `public Task<int> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult(42);` → `public ValueTask<int> Handle(Ping request, CancellationToken cancellationToken) => ValueTask.FromResult(42);` |
| `edge-inactive-if` | Fixed | `public Task<int> Handle(Query request, CancellationToken cancellationToken) return Task.FromResult(request.Id)…` → `public ValueTask<int> Handle(Query request, CancellationToken cancellationToken) return ValueTask.FromResult(r…` |
| `edge-marker-interface` | Fixed | `public async Task<int> Handle(Query request, CancellationToken cancellationToken)` → `public async ValueTask<int> Handle(Query request, CancellationToken cancellationToken)` |
| `edge-nested-generic` | Fixed | `public async Task<Result<List<int>>> Handle(Query request, CancellationToken cancellationToken)` → `public async ValueTask<Result<List<int>>> Handle(Query request, CancellationToken cancellationToken)` |
| `edge-own-command-name` | Fixed | `public async Task<int> Handle(Ping request, CancellationToken cancellationToken) public async Task<int> Execut…` → `public async ValueTask<int> Handle(Ping request, CancellationToken cancellationToken) public async Task<int> E…` |
| `edge-own-command-name-global` | Fixed | `global using MediatR; public async Task<int> Handle(Ping request, CancellationToken cancellationToken)` → `global using Mediator; public async ValueTask<int> Handle(Ping request, CancellationToken cancellationToken)` |
| `edge-primary-ctor` | Fixed | `public Task<int> Handle(Query request, CancellationToken cancellationToken) => Task.FromResult(request.Id + cl…` → `public ValueTask<int> Handle(Query request, CancellationToken cancellationToken) => ValueTask.FromResult(reque…` |
| `explicit` | Manual | `Task<int> IRequestHandler<Query, int>.Handle(Query request, CancellationToken cancellationToken) =>` — reden: explicit interface implementation |
| `from-result-block` | Fixed | `public Task<int> Handle(Query request, CancellationToken cancellationToken) return Task.FromResult(0);` → `public ValueTask<int> Handle(Query request, CancellationToken cancellationToken) return ValueTask.FromResult(0…` |
| `from-result-expression` | Fixed | `public Task<int> Handle(Query request, CancellationToken cancellationToken) => Task.FromResult(request.Id);` → `public ValueTask<int> Handle(Query request, CancellationToken cancellationToken) => ValueTask.FromResult(reque…` |
| `generic` | Fixed | `public Task<T> Handle(Wrapped<T> request, CancellationToken ct) => Task.FromResult(request.Value);` → `public ValueTask<T> Handle(Wrapped<T> request, CancellationToken ct) => ValueTask.FromResult(request.Value);` |
| `null-return` | Manual | `public Task<int> Handle(Query request, CancellationToken cancellationToken) => null!;` — reden: not a Task<T> |
| `other-task` | Fixed | `public Task<int> Handle(Query request, CancellationToken cancellationToken) => Task.Run(() => request.Id, canc…` → `public ValueTask<int> Handle(Query request, CancellationToken cancellationToken) => new ValueTask<int>(Task.Ru…` |
| `token-name` | Fixed | `public async Task<int> Handle(Query query, CancellationToken token)` → `public async ValueTask<int> Handle(Query query, CancellationToken token)` |

### RequestHandlerDelegate (5)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `callback-returns` | Manual | `public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) => next()…` — reden: is a Task or a callback, not the response |
| `invoke-call` | Manual | `public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) => next()…` — reden: is used other than as a mock configuration or as the next argument |
| `lambda` | Manual | `public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) => next()…` — reden: is initialized with a real delegate, not a Substitute.For mock |
| `no-handle-call` | Manual |  — reden: is not handed to a behavior's Handle |
| `parameter` | Manual | `public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) => next()…` — reden: is not a field or local that is mocked or handed to a behavior |

### RequestProcessor (5)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `other-return` | Fixed | `public class DelayPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull public Task…` → `public class DelayPreProcessor<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse> where TRequest …` |
| `post-open-async` | Fixed | `public class AuditPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse> where TReque…` → `public class AuditPostProcessor<TRequest, TResponse> : MessagePostProcessor<TRequest, TResponse> where TReques…` |
| `pre-closed` | Fixed | `public class QueryPreProcessor : IRequestPreProcessor<Query> public Task Process(Query request, CancellationTo…` → `public class QueryPreProcessor : MessagePreProcessor<Query, int> protected override ValueTask Handle(Query req…` |
| `pre-open` | Fixed | `public class AuditPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull public Task…` → `public class AuditPreProcessor<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse> where TRequest …` |
| `task-run` | Fixed | `public class LoggingProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull public Task …` → `public class LoggingProcessor<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse> where TRequest :…` |

### RequestProcessorCaller (3)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `result-used` | Manual |  — reden: is not a plain `await x.Process(...);` statement |
| `through-interface` | Manual |  — reden: is called through the MediatR interface |
| `unknown-response` | Manual |  — reden: the response type of T is unknown |

### SendAsTask (5)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `nsubstitute-returns` | Fixed | `_mediator.Send(Arg.Any<IRequest<bool>>(), default).Returns(Task.FromResult(true)); _mediator.Send(Arg.Any<Quer…` → `_mediator.Send(Arg.Any<IRequest<bool>>(), default).Returns(ValueTask.FromResult(true)); _mediator.Send(Arg.Any…` |
| `nsubstitute-returns-lambda` | Manual |  — reden: set up on an NSubstitute mock |
| `null-conditional` | Manual |  — reden: called with ?. |
| `send-object` | Fixed | `return _mediator.Send(message, cancellationToken);` → `return _mediator.Send(message, cancellationToken).AsTask();` |
| `task-uses` | Fixed | `public Task<int> GetAsync(int id, CancellationToken cancellationToken) => _sender.Send(new Query(id), cancella…` → `public Task<int> GetAsync(int id, CancellationToken cancellationToken) => _sender.Send(new Query(id), cancella…` |

### StreamPipelineBehavior (3)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `next-passed` | Manual | `public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, Cancellatio…` — reden: next is used other than as next() |
| `open` | Fixed | `public class StreamLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse> where T…` → `public class StreamLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse> where T…` |
| `request-constraint` | Fixed | `public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [Enum…` → `public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TRequest, TResponse> n…` |

### Unsupported (3)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `exception-action` | Manual |  — reden: run the action and return NotHandled |
| `generic-handler` | Manual | `public Task<int> Handle(CountQuery<TEntity> request, CancellationToken cancellationToken) => Task.FromResult(0…` — reden: does not support generic messages or request handlers |
| `notification-publisher` | Manual |  — reden: set NotificationPublisherType in AddMediator |

### VoidRequestHandler (10)

| Proof | Uitkomst | Wat het bewijst |
| --- | --- | --- |
| `async` | Fixed | `public async Task Handle(Command request, CancellationToken cancellationToken)` → `public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken) return Unit.Value;` |
| `async-expression` | Manual | `public async Task Handle(Command request, CancellationToken cancellationToken) =>` — reden: without a statement body |
| `block-end` | Fixed | `public async Task Handle(Command request, CancellationToken cancellationToken)` → `public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken) return Unit.Value;` |
| `completed-task` | Fixed | `public Task Handle(Command request, CancellationToken cancellationToken) return Task.CompletedTask;` → `public ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken) return Unit.ValueTask;` |
| `completed-task-expression` | Fixed | `public Task Handle(Command request, CancellationToken cancellationToken) => Task.CompletedTask;` → `public ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken) => Unit.ValueTask;` |
| `early-return` | Fixed | `public async Task Handle(Command request, CancellationToken cancellationToken) return;` → `public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken) return Unit.Value;` |
| `edge-marker-interface` | Fixed | `public async Task Handle(Command request, CancellationToken cancellationToken)` → `public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken) return Unit.Value;` |
| `explicit` | Manual | `Task IRequestHandler<Command>.Handle(Command request, CancellationToken cancellationToken) => Task.CompletedTa…` — reden: explicit interface |
| `other-return` | Manual | `public Task Handle(Command request, CancellationToken cancellationToken) =>` — reden: not Task.CompletedTask |
| `throw-at-end` | Fixed | `public async Task Handle(Command request, CancellationToken cancellationToken)` → `public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken)` |

## Zie ook

- `docs/projectinfo/architectuur/testen.md` — hoe proofs draaien
- `docs/projectinfo/architectuur/edge-cases.md` — herkomst van de edge-proofs
