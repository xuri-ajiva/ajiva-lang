# The Ajiva-Lang Specification

basic description

## File formats

Ajiva Source Code are [UTF-8](https://en.wikipedia.org/wiki/UTF-8) files with the `.aj` file extension.
The Source Files are compiled with the `ajivac` compiler into `.alib`.

# Basic concept

The Ajiva use the concept context different meaning for characters.
e.g.

```aj
i32 a = 10 // assign a to 10
if a = 20 // comparison
    a = 10 << 2 // assign a to be 10 << 2 witch is 20
```

## Naming

A name is only allowed if a name is expected, allowed characters are `[a-z,A-Z,_]`.
e.g.

```aj
str hello = "World!"
```

## Sections

- Ajiva uses section as the term for a region inside the source.
- A section can be the file it self, a folder containing the file beginning at the folder of the compilation process.
- A section can also be the block after an if statement.
- Therefore a section can allow many different declarations.
- Section Names must follow the [Naming](#naming) Specification
  - All non allowed characters are removed
  - All spaces are converted to underscores (`_`)
  - Two or more underscores in a row are converted to one.
- Sections are defined by Markers.
  - Begin / end of Section can have a different maker
  - Multi line sections starting with the begin maker followed by a star (`*`) continue until star end maker or an `EOF` token.

| section name                              | allowed statements / definitions | Begin Marker | End Marker |
| ----------------------------------------- | -------------------------------- | ------------ | ---------- |
| File                                      | functions, UDF                   | EOF          | EOF        |
| Folder                                    | files, no other posable          | EOF          | EOF        |
| Function                                  | functions, variables, UDF        | {            | }          |
| Control flow statements <br> with section | sane as function                 | {            | }          |
| Comment Multi Line                        | no Subsections Parsed            | //\*         | /\*/       |
| Comment single line                       | no Subsections Parsed            | //           | \n OR \r   |

## Comments

Comments are sections which are not interpreted as code.
Comments Use the `\` character as the section markers meaning
line Comments begin with `//` and block comments start with `/*` and must end in `*/` or `EOF`

```aj
// This is a line Comment

/*
And this is a Block comment
You can write multiple lines in their
*/
```

## Namespace

A namespace is considered the absolute path to a function / variable / user defined structure.
The namespace is automatic based on the context.
The namespace is the name of every section separated with a dot from top to bottom

e.g.
root/file.aj

```aj
// current namespace: root.file

#pure  // wrap next block in a pure function named after the file, one allowed per file
{* // block until EOF
// current namespace: root.file.file
```

## Variables

Uses the Standard [Naming](#naming) Specification
Variables are staticky typed and annotated meaning you must tell the compiler witch type a variable has.
build in types are:
|keyword|description|
|-------|-----------|
| `i32` | integer 32 bits |
| `i64` | integer 64 bits |
| `f32` | floating point 32 bits of presidios |
| `f64` | floating point 64 bits of presidios |
| `chr` | character encoded in UTF-8 |
| `str` | character array |
| `bit` | single bit representing either True of False |

e.g.

```aj
TODO
```

## Operators

### Assignment

Ajiva uses the `=` to assign a value to a variable.
```aj
i32 a = 10 // set a to 10
str b = "hello" // set b to "hello"
```

## Control flow

Control flow can be done by many different statements

| statement | expects                | meaning                                                     |
| --------- | ---------------------- | ----------------------------------------------------------- |
| if        | expression --> section | checks the expression and branches into the section if true |
| while     | expression --> section | loops the given block as long as the expression is true     |

## Functions

Functions are allowed to be defined in every section and must contain a function body as section.

## User Defined Structures

## Attributes

Attributes start with `@` or `#` or `^` depending on the type of the attribute.

e.g

```aj
@entry // defines entry point
#pure  // wrap next block in a pure function named after the file, one allowed per file
```
