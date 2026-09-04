# Contributing

Bug reports based on real Word/WPS output are especially valuable.

Please include:

1. EndNote version and Word/WPS version;
2. selected ENS style;
3. EndNote reference type;
4. relevant field values, with private information removed;
5. actual footnote output;
6. expected footnote output and the rule or example supporting it.

Do not upload unpublished manuscripts, personal libraries, access credentials, or documents containing private information.

Before opening a pull request, run:

```bash
python -m pip install pytest
python -m pytest -q
```

Generated citation rules and style data must preserve the CC BY-SA 3.0 attribution and share-alike terms described in `LICENSE`.
