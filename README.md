# AAZ-Mobile — HD-2D

Work toward an HD-2D version of AAZ-Mobile (Unity).

## Status

The **rendering layer is built** and lives in [`Packages/com.aaz.hd2d`](Packages/com.aaz.hd2d).
It is engine-side work that does not depend on the game's own content, so it was
possible to complete before seeing the project.

The **project-specific conversion has not started**, because the source build is
not reachable from this environment — see below.

## Blocker: the source build

The original project was shared as a `pixeldrain.com` link. Outbound HTTPS in this
session goes through a policy-enforcing egress proxy, and that host is not on the
allowlist:

```
"kind": "connect_rejected",
"detail": "gateway answered 403 to CONNECT (policy denial or upstream failure)",
"host": "pixeldrain.com:443"
```

`pixeldrain.net` and `pixeldra.in` are blocked the same way. Routing around an
organization egress denial is not something to work around locally, so the file
has to be made reachable instead. Any one of these unblocks it:

- an admin allowlists `pixeldrain.com` for this environment, or
- the project is pushed to this repository, or
- it is re-hosted somewhere already reachable (a GitHub release, for instance).

## What happens once the project is available

See [`docs/CONVERSION-PLAN.md`](docs/CONVERSION-PLAN.md) for the staged plan and
the mobile performance budget.
