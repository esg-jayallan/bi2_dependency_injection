# bi2_injection
The Dependency Injection system we use in Bubble Island 2 (Unity3D / C#)

## History

When Bubble Island 2 started out, it was using StrangeIoC as a framework. Over time we became concerned with its runtime performance. Nonetheless, it had warmed us to the concept of Dependency Injection (DI) and thus we rolled our own Injector class that we could optimize for our use case.

## Overview

There are excellent primers on DI on the internet. For fear of broken links, we refer you to your favorite search engine.

## Differences to other injectors

### Fields only
We only ever inject fields, due to the overhead of calling a property setter via reflection.

### Explicit injectable marker interface
All classes (or structs, if you feel adventurous) that are to be injected must be marked with the `IInjectable` marker interface. Don't worry, we don't call any functions through it, so there shouldn't be any performance impact.

### Injected fields must be `protected`
This is enforced by the `TestAuditProtectionLevel` unit test which reflects over all classes in the same Assembly as `Injector` and finds injected fields. The reasoning behind that is as follows:

Public fields would allow anyone to overwrite the injected members, not something we want.

If fields were private, a derived class could shadow that field with its own private member of the same name - a common occurrence given field names are usually derived from the name of the injected class (e.g. `IReachabilityService` becomes `_reachabilityService` or `ReachabilityService`). To keep the injector simple, it does not traverse the class hierarchy and thus the field in the parent class would not be injected (remain `null`).

In any case, parent and child would ultimately contain the same reference, which would be a waste of a reflected set call.

Thus the enforcement of all injected fields as `protected`.

### No Unbind or Rebind
Once it's in the injector, you cannot remove an object from its bindings. See "Contexts" in the "Usage" section for the recommended best practice.

### Lean
This is not an MVCs framework. We don't create instances for you on the spot. We don't have magic features. All the system does is set fields in the objects its given based on what has been bound before.

## Usage

### Basic usage

A single `Injector` holds a mapping from `Type` to `object`. You can add to that mapping by calling `injector.Bind<Type>(object)`. Keep in mind that the type must be `IInjectable`.

```
public class InjectableA : IInjectable
{
}

Injector injector = new Injector();
InjectableA theA = new InjectableA();
injector.Bind<InjectableA>(theA);
```

Now create a class and mark the members you want injected with the `[Inject]` attribute. Calling `injector.Inject` on an instance of that class will fill these fields with the bound instances.

```
public class Injected
{
	[Inject] protected InjectableA a;
}

Injected injected = new Injected();
injector.Inject(injected);

// injected.a now contains theA from above

```

### PostBindings

After setting up all of your bindings, you may want all the objects you passed in to be injected in turn. You can do this by calling `PostBindings` on the injector.

```
public class InjectableB : IInjectable
{
	[Inject] protected InjectableA a;
}
InjectableB theB = new InjectableB();
injector.Bind<InjectableB>(theB);
injector.PostBindings();

// theB.a now contains theA from above
```

*Warning:* `PostBindings` should be called once and only once for each Injector, although this is not enforced. More specifically, `PostBindings` does NOT call the parent injector's `PostBindings` assuming that that has already been called. See the "Contexts" section below.

### GetInstance

You can ask the injector to return you a previously bound object. This is just a dictionary lookup. In the above example:

```
injector.GetInstance<InjectableA>() // returns theA
```

### Injector inheritance

An `Injector` can be handed a parent injector in its constructor. All bindings from all parents are available to this injector.

```
Injector parentInjector = new Injector();
Injector childInjector = new Injector(parentInjector);
parentInjector.Bind<InjectableA>(theA);
childInjector.GetInstance<InjectableA>() // returns theA
```

Bindings in the child can override those in the parent, although this is not recommended.

```
// continued example
InjectableA anotherA = new InjectableA();
childInjector.Bind<InjectableA>(anotherA);
childInjector.GetInstance<InjectableA>() // returns anotherA
```

### Contexts

StrangeIoC introduces the concept of Contexts to isolate bindings between different application modules. In particular, we found the "CrossContext" concept inadequate for what we wanted to do. Let's consider a game made up of a 'core' gameplay and a 'menu' module. Both of these share some components (services, especially).

For each of these modules (contexts), we create an injector whose parent is that of the parent context:

```
public class ApplicationContext
{
	Injector injector = new Injector();

	... setup all bindings

	injector.PostBindings();
}

public class CoreGameplayContext
{
	Injector injector = new Injector(applicationContext.injector);

	... setup all bindings

	injector.PostBindings();
}

public class MenuContext
{
	Injector injector = new Injector(applicationContext.injector);

	... setup all bindings

	injector.PostBindings();
}
```

By tying the child context's lifecycle to the Unity scene which displays it, we ensure that all the references in a child context are released at the moment we leave that module (e.g. leave the core gameplay). For this reason we never need to Unbind anything, instead, the whole injector gets thrown away.

## Caveats

The `IInjectable` interface is marked with the `[MeansImplicitUse]` attribute to silence Resharper's warnings that noone ever sets these fields. Unfortunately this also silences the warning that noone ever *reads* the field. Over time, this can lead to unused injections in your classes each of which incur a slight performance hit. When refactoring, keep an eye open for injects that are no longer used.

It's also worth repeating the warning about `PostBindings` not working recursively.

## Acknowledgements

This injection system was built to be plug-in replaceable with [StrangeIoC](http://strangeioc.github.io/strangeioc/)'s (for our use case). Design considerations and nomenclature are owed to it.

Made with <3 at [Wooga](https://www.wooga.com) for [Bubble Island 2](https://www.wooga.com/games/bubble-island/)

