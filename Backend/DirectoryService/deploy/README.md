# Deployment VM

This is a separate learning stack for the Ubuntu VM, not a replacement for the
development Compose file. Its project name is `directory-deploy`; its network
and named volumes are managed by that project. No application is built here.

## Configuration

Transfer this directory to the VM, preserving `compose.yml`, `nginx.conf`, and
`.env.example` together. Run the following commands inside that directory in
Ubuntu, not in the development stack on macOS:

```sh
cp --update=none .env.example .env
chmod 600 .env
nano .env
```

The sample uses `IMAGE_TAG=latest`. The `publish-manifests` job updates this tag
after creating and inspecting both SHA-tagged application image indexes. Both
application images use common multi-platform tags. Leave platform selection to
Docker so the ARM64 VM pulls the ARM64 variants.

To pin a release or return to an older application image, set `IMAGE_TAG` to
`sha-<full commit SHA>` from a successful `publish-manifests` job. Do not include
an `-amd64` or `-arm64` suffix.

Set separate `POSTGRES_PASSWORD` and `SEQ_PASSWORD` values, for example using
`openssl rand -hex 32` twice. Do not commit `.env`; the repository already
ignores it. The sample intentionally has no working default passwords.

The frontend image must have been built with `NEXT_PUBLIC_API_URL=/api/`.
Changing a runtime variable cannot change that compiled browser setting.

If GHCR packages are private, authenticate inside the VM using
`sudo docker login ghcr.io` and a token with `read:packages`. Do not use the GitHub account
password, put tokens in Compose, or paste them into shell commands or chat.
Public packages can be pulled without authentication.

## First deployment

```sh
sudo docker compose --env-file .env config --quiet
sudo docker compose --env-file .env pull
sudo docker compose --env-file .env up -d --no-build --wait --wait-timeout 180
sudo docker compose ps
```

Open `http://<VM-IP>:8080` from the Mac. Nginx routes `/` to Next.js and `/api/`
to the backend, preserving the API prefix. PostgreSQL, Redis, and Seq are not
published on VM ports. The nginx `/health` checks nginx only; application
healthchecks are shown separately by `docker compose ps`.

For startup failures, inspect `sudo docker compose logs --tail=100 backend`
or replace `backend` with the failing service name.

## Updates and data

With `IMAGE_TAG=latest`, leave `.env` unchanged and repeat `pull` and `up` after
publication. A registry push alone never updates running containers. There is
no need to run `down` first. Named volumes preserve the data when containers
are replaced; `docker compose down -v` deletes those volumes.

`latest` is mutable and represents the last promotion, not an immutable commit
identifier. Concurrent builds can finish out of commit order. Backend and
frontend aliases are also updated sequentially, not atomically. For a precise
paired release, use the common SHA tag instead.

The backend applies database migrations on startup. Back up important data
before updating: reverting an image does not undo database migrations. Database
and Seq initialization passwords apply to fresh data volumes; changing `.env`
alone does not rotate credentials in an existing database or Seq instance.

Infrastructure tags in the sample can change over time. For stricter
reproducibility, set their image references to tested digests. This learning
stack is HTTP-only and is not a hardened public production deployment.
