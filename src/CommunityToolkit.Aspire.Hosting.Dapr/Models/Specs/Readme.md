# How to generate the schema.json

The file `component-metadata-schema.json` comes from [here](https://github.com/dapr/components-contrib/blob/main/component-metadata-schema.json)

The other `*.md` come from [here](https://github.com/dapr/docs/tree/v1.14/daprdocs/content/en/reference/resource-specs)

Then you just start a CoPilot `Edits` session with the following prompt:

```
Can you extract out of all these documentation files all described schemas as json schema? Please be very percise and respect not only what is written in the YAML code blocks but always also the things in the tables. Don't forget any property from the YAML codeblock. Have fun :)
```

This will generate the missing `*-schema.json` files.

It's probably not perfect but it's a good start.
