# Supported patterns

Every rewrite Unshackle knows, per fix. A pattern rewrites a call only when the result is provably the same; otherwise it reports
the spot as **Manual** with the reason (see `--report`). Each pattern has before/after proofs in
[`src/Unshackle.Tests/Proofs/`](https://github.com/ATkingma/unshackle/tree/main/src/Unshackle.Tests/Proofs).

How to run a fix: [usage.md](usage.md). The tables below are generated (`unshackle list --markdown`); a test keeps them in sync with the code.

<!-- generated: unshackle list --markdown -->

## fluentassertions

| Pattern | What it does |
| --- | --- |
| `fa.custom-assertion` | x.Should().BeCancelled() on an own ReferenceTypeAssertions class → x.ShouldBeCancelled(); other custom assertions → Manual, grouped |
| `fa.assertion-scope` | using (new AssertionScope()) { a; b; } → x.ShouldSatisfyAllConditions(() => a, () => b); otherwise Manual |
| `fa.all-satisfy` | x.Should().AllSatisfy(a => ...) → x.ShouldNotBeEmpty(); foreach (var a in x) { ... } |
| `fa.discard-assignment` | _ = x.Should().Be(y); → x.Should().Be(y); (then rewritten by the assertion patterns) |
| `fa.invoking-local` | var act = x.Invoking(s => s.M()); → Action act = () => x.M(); (then rewritten by the Throw/NotThrow patterns) |
| `fa.and-chain` | x.Should().A().And.B() → x.ShouldA(); x.ShouldB(); |
| `fa.should-be` | x.Should().Be(y) → x.ShouldBe(y) |
| `fa.not-be` | x.Should().NotBe(y) → x.ShouldNotBe(y) |
| `fa.have-count` | x.Should().HaveCount(n) → x.Count.ShouldBe(n), HaveCountGreaterThan(n) → x.Count.ShouldBeGreaterThan(n) |
| `fa.be-true-false` | x.Should().BeTrue() → x.ShouldBeTrue() (bool? → x.ShouldBe(true)) |
| `fa.be-null` | x.Should().BeNull() → x.ShouldBeNull(), NotBeNull() → ShouldNotBeNull() |
| `fa.be-empty` | x.Should().BeEmpty() → x.ShouldBeEmpty(), NotBeEmpty() → ShouldNotBeEmpty(); on a Guid → ShouldBe(Guid.Empty) / ShouldNotBe(Guid.Empty) |
| `fa.contain` | x.Should().Contain(item\|predicate) → x.ShouldContain(item\|predicate), NotContain → ShouldNotContain |
| `fa.be-equivalent-to-string` | s.Should().BeEquivalentTo("x") → s.ShouldBe("x", StringCompareShould.IgnoreCase) |
| `fa.throw` | act.Should().Throw<T>()[.WithMessage(p)/.Where(..)/.WithInnerException<TI>()][.And/.Which] → Should.Throw<T>(act)[.Message.Should...] |
| `fa.throw-async` | await act.Should().ThrowAsync<T>()[.WithMessage(p)/.Where(..)/.WithInnerException<TI>()] → await Should.ThrowAsync<T>(act)[.Message.Should...] |
| `fa.throw-exactly` | act.Should().ThrowExactly<T>() → Should.Throw<T>(act).ShouldBeOfType<T>() (and the async form) |
| `fa.invoking` | x.Invoking(s => s.M()).Should().Throw<T>() / Awaiting / NotThrow → Should.Throw<T>(() => x.M()) etc. |
| `fa.be-greater-less` | x.Should().BeGreaterThan(n) → x.ShouldBeGreaterThan(n), BeInRange(a, b) → ShouldBeInRange(a, b), BeAfter(d) → ShouldBeGreaterThan(d) |
| `fa.not-throw` | act.Should().NotThrow() → Should.NotThrow(act) |
| `fa.contain-key` | dict.Should().ContainKey(k) → dict.ShouldContainKey(k), NotContainKey → ShouldNotContainKey |
| `fa.be-equivalent-to-object` | obj.Should().BeEquivalentTo(other) → obj.ShouldBeEquivalentTo(other) for the same plain type; a collection of primitives → ShouldBe(other, ignoreOrder: true); otherwise Manual |
| `fa.equivalent-options` | x.Should().BeEquivalentTo(y, o => o.WithStrictOrdering()) on a collection of values → x.ShouldBe(y); no-op options are dropped; every other option is Manual |
| `fa.contain-equivalent-of` | x.Should().ContainEquivalentOf(item) → x.ShouldContain(item), NotContainEquivalentOf → ShouldNotContain, for value-compared items of the element type; otherwise Manual |
| `fa.only-contain` | x.Should().OnlyContain(pred) → x.ShouldNotBeEmpty(); x.ShouldAllBe(pred); (FluentAssertions fails on an empty collection) |
| `fa.be-of-type` | x.Should().BeOfType<T>()[.Which] → x.ShouldBeOfType<T>() |
| `fa.string-start-end` | s.Should().StartWith(x) → s.ShouldStartWith(x, Case.Sensitive), NotBeNullOrEmpty() → ShouldNotBeNullOrEmpty() |
| `fa.contain-single-which` | x.Should().ContainSingle() → x.ShouldHaveSingleItem(), ContainSingle(p) → x.ShouldContain(p, 1), var y = ….Which → var y = x.Where(p).ShouldHaveSingleItem() |
| `fa.equal-sequence` | x.Should().Equal(seq) / Equal(1, 2) → x.ShouldBe(seq) / x.ShouldBe(new[] { 1, 2 }) (order-sensitive); on a dictionary of value items → x.ShouldBe(dict, ignoreOrder: true) |
| `fa.not-equal` | x.Should().NotEqual(seq) → x.ShouldNotBe(seq) (order-sensitive; explicit IEnumerable<T> when the types differ) |
| `fa.order` | x.Should().BeInAscendingOrder(p => p.K) → x.Select(p => p.K).ShouldBeInOrder(SortDirection.Ascending); BeInDescendingOrder likewise |
| `fa.not-contain-nulls` | x.Should().NotContainNulls() → x.ShouldNotContain(i => i == null); x.Should().AllBe(v) → x.ShouldAllBe(i => i == v) (value-like elements only) |
| `fa.have-length` | s.Should().HaveLength(n) → s.Length.ShouldBe(n) |
| `fa.be-approximately` | x.Should().BeApproximately(y, tol) / BeCloseTo(t, span) → x.ShouldBe(y, tol); NotBe… → ShouldNotBe or Math.Abs(x - y).ShouldBeGreaterThan(tol) |
| `fa.be-same-as` | x.Should().BeSameAs(y) → x.ShouldBeSameAs(y), NotBeSameAs → ShouldNotBeSameAs |
| `fa.not-throw-async` | await act.Should().NotThrowAsync() → await Should.NotThrowAsync(act); x.Awaiting(s => s.M()) → () => x.M() |
| `fa.match` | s.Should().MatchRegex(r) → s.ShouldMatch(r), Match("SO-*") → s.ShouldMatch(@"(?s)\ASO-.*$") |
| `fa.direct-mapping` | BeOneOf → ShouldBeOneOf, BeSubsetOf → ShouldBeSubsetOf, OnlyHaveUniqueItems → ShouldBeUnique, HaveValue/NotHaveValue → ShouldNotBeNull/ShouldBeNull |
| `fa.catch-assertion-exception` | catch (AssertionFailedException) / e is XunitException → also catches ShouldAssertException |
| `fa.timespan-extensions` | 5.Seconds() / 200.Milliseconds() → TimeSpan.FromSeconds(5) / TimeSpan.FromMilliseconds(200); 1.March(2020) → new DateTime(2020, 3, 1); 1.March(2020).At(12, 30) → new DateTime(2020, 3, 1, 12, 30) |

## mediatr

| Pattern | What it does |
| --- | --- |
| `mediatr.send-as-task` | Send/Publish, or a direct handler.Handle(...), whose result is used as a Task (not awaited directly) → .AsTask(); Mediator returns ValueTask. An NSubstitute setup mock.Send(...).Returns(Task.FromResult(x)) → .Returns(ValueTask.FromResult(x)) |
| `mediatr.request-handler` | IRequestHandler<TReq, TRes>: Task<TRes> Handle(...) → ValueTask<TRes> Handle(...), Task.FromResult → ValueTask.FromResult, another Task<TRes> → new ValueTask<TRes>(task) |
| `mediatr.void-request-handler` | IRequestHandler<TReq>: Task Handle(...) → ValueTask<Unit> Handle(...) with return Unit.Value |
| `mediatr.notification-handler` | INotificationHandler<T>: Task Handle(...) → ValueTask Handle(...), Task.CompletedTask → ValueTask.CompletedTask, another Task → new ValueTask(task) |
| `mediatr.request-handler-delegate` | RequestHandlerDelegate<TRes> handed to a behavior's Handle (NSubstitute mock, field, real lambda or Moq Mock<RequestHandlerDelegate<TRes>>) → MessageHandlerDelegate<TReq, TRes>: Invoke() → Invoke(Arg.Any<TReq>(), Arg.Any<CancellationToken>()), _ => Task → (_, _) => ValueTask; anything else → Manual with the line |
| `mediatr.pipeline-behavior` | IPipelineBehavior<TReq, TRes>: Task<TRes> Handle(request, RequestHandlerDelegate<TRes> next, ct) → ValueTask<TRes> Handle(request, MessageHandlerDelegate<TReq, TRes> next, ct), next() → next(request, ct), TReq : IMessage |
| `mediatr.stream-pipeline-behavior` | IStreamPipelineBehavior<TReq, TRes>: StreamHandlerDelegate<TRes> next → StreamHandlerDelegate<TReq, TRes> next, next() → next(request, ct), TReq : IStreamMessage |
| `mediatr.request-processor-caller` | Use of a rewritten request processor (tests): P<TReq> → P<TReq, TRes>, await p.Process(req, ct) → await p.Handle(req, (_, _) => default, ct); anything else → Manual with the line |
| `mediatr.request-processor` | IRequestPreProcessor<TReq> / IRequestPostProcessor<TReq, TRes>: Task Process(...) → MessagePreProcessor<TReq, TRes> / MessagePostProcessor<TReq, TRes> with protected override ValueTask Handle(...) |
| `mediatr.exception-handler` | IRequestExceptionHandler<TReq, TRes, TEx>: Task Handle(request, exception, state, ct) with state.SetHandled(x); return Task.CompletedTask → MessageExceptionHandler<TReq, TRes, TEx> with return new(Handled(x)) / new(NotHandled) |
| `mediatr.add-mediatr` | services.AddMediatR(cfg => ...) → services.AddMediator(cfg => ...): assembly registration dropped, behaviors, processors and exception handlers → PipelineBehaviors, stream behaviors → StreamPipelineBehaviors, Lifetime → ServiceLifetime |
| `mediatr.behavior-di-registration` | services.AddTransient/AddScoped/AddSingleton(typeof(IPipelineBehavior<,>), typeof(X<,>)) → unchanged; Mediator 3 resolves behaviors from DI, the using swap does the rest |
| `mediatr.autofac-mediator-module` | builder.RegisterAssemblyTypes(typeof(IMediator).Assembly) in an Autofac module → AddMediator on a ServiceCollection, Populate'd without its handler registrations; MediatR.Pipeline types leave the scan lists, RegisterGeneric(RequestPre/PostProcessorBehavior) goes |
| `mediatr.generic-decorator` | Open generic handler decorator D<T> : IRequestHandler<T> (Autofac RegisterGenericDecorator) → decorated IRequestHandler<T, Unit> and typeof(IRequestHandler<,>) as the decorated service; a notification decorator stays when MediatR is registered by hand in Autofac |
| `mediatr.generic-request` | class G<T, R> : IRequest<R> → abstract G<T, R> + class GOfXAndY : G<X, Y> per closed type, G<X, Y> → GOfXAndY, inherited handler gets IRequestHandler<GOfXAndY, Y>; a generic message that is only a base class → abstract |
| `mediatr.generic-notification` | class G<T> : INotification → abstract G<T> + class GOfX : G<X> per closed type; new G<X>(...) → new GOfX(...), typeof(G<>).MakeGenericType(x) → GTypes.Close(x) |
| `mediatr.mediator-implementation` | No-op class : IMediator / ISender / IPublisher → Mediator 3.0.2's members (ValueTask, ICommand/IQuery/stream overloads) as no-ops |
| `mediatr.unsupported` | MediatR exception actions, notification publishers, concrete generic messages and request handlers, and packages that build on MediatR → Manual with advice |
| `mediatr.internal-handler-access` | internal handlers scanned by AddMediatR in another project → [assembly: InternalsVisibleTo("<that project>")], so the code Mediator generates there can reach them |
| `mediatr.qualified-swapped-type` | MediatR.IRequest<T> (a swapped type written with its namespace) → Mediator.IRequest<T> |

## automapper

| Pattern | What it does |
| --- | --- |
| `automapper.create-map` | Profile with CreateMap<A, B>() without options → static B ToB(this A source) => new() { X = source.X, ... } for the members with the same name and type (or a built-in conversion AutoMapper does the same way); MapInto(this A source, B destination) when the solution calls Map(source, destination); AToBProjection when the solution calls ProjectTo; Map with options is Manual |
| `automapper.for-member-ignore` | CreateMap<A, B>().ForMember(d => d.X, o => o.Ignore()) → B ToB(this A source) without X; other options and unmatched members stay Manual |
| `automapper.for-member-map-from` | CreateMap<A, B>().ForMember(d => d.X, o => o.MapFrom(s => expr)) → B ToB(this A source) with X = expr; expressions that may throw on null or need a conversion stay Manual |
| `automapper.value-resolver` | ForMember(d => d.X, o => o.MapFrom<TResolver>()) and MapFrom((s, d) => expr) → X = new TResolver().Resolve(source, default!, default!, default!) or X = expr in B ToB(this A source); resolvers with DI dependencies or that use the destination, member or context stay Manual |
| `automapper.kept-map` | CreateMap<A, B>() left in a trimmed profile by the partial migration → Manual (stays on AutoMapper), with the reason; changes nothing |
| `automapper.map-call` | mapper.Map<B>(a) → a.ToB(), calling the extension method of CreateMap<A, B>; Map<List<B>>(items) → items.Select(x => x.ToB()).ToList(); unknown maps are Manual |
| `automapper.generic-map` | mapper.Map<T>(x) on a type parameter → typeof(T) == typeof(B) ? (T)(object)x.ToB() : … over the closed type arguments of every caller; open sets are Manual |
| `automapper.generic-profile` | Generic Profile P<T> with CreateMap<T, T>() → B ToB(this A source) per closed type argument (closed subclasses, new P<X>(), AddProfile<P<X>>); open sets are Manual |
| `automapper.map-into` | mapper.Map(a, b) → a.MapInto(b), calling the MapInto that the profile rewrite writes for CreateMap<A, B>; collections, unknown maps and a maybe-null source or destination are Manual |
| `automapper.project-to` | query.ProjectTo<B>(mapper.ConfigurationProvider) → query.Select(ProfileExtensions.AToBProjection), an Expression<Func<A, B>> the profile rewrite writes next to ToB; parameters, members to expand and unknown maps are Manual |
| `automapper.mock-set-up` | Stub of IMapper.Map in a test whose value provably equals the generated mapping → removed (the class under test calls the generated method) |
| `automapper.unused-imapper` | IMapper field, constructor parameter or local that only served migrated Map calls → removed, with its assignments, null checks and the arguments at every caller |
| `automapper.add-automapper` | services.AddAutoMapper(...); → removed when no IMapper (or IConfigurationProvider) is used anywhere; otherwise, or when the result is used, Manual |
| `automapper.configuration-test` | AssertConfigurationIsValid() and test mappers built from profiles (new MapperConfiguration(cfg => cfg.AddProfile<P>()).CreateMapper()) → removed; a configuration with its own maps or settings, or a check while maps outside profiles remain, is Manual |
