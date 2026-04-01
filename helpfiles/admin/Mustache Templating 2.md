Mustache templating is an industry standard templating system used by the system.

Mustache Templating creates html documents from a layout and a hash. The layout includes Mustache Tags that the system updates from the hash.
Typically the layout is stored in a Layout record and the hash is created by the system from related data records.

### A simple Mustache Template Example

Hello {{name}}
You have just won {{value}} dollars!

This template includes two Mustache Tags, {{name}} and {{value}}

The system might produce the following hash based on database records.

{
  "name": "Chris",
  "value": 10000,
}

Will produce the following:

Hello Chris
You have just won 10000 dollars!

### References

[https://mustache.github.io/](https://mustache.github.io/)
[Mustache Specification](https://mustache.github.io/mustache.5.html)