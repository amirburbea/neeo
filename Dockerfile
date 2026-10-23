# Builds the Neeo SDK server image, with the Hisense, Kodi, Plex and WebOS drivers published alongside it.
# Which drivers actually run is chosen at runtime with NEEO_Drivers (see below), so one image serves every setup.
#
# Run it with host networking so Brain and device discovery (mDNS), wake-on-LAN and the ARP lookups work, and
# so the Brain can reach the server. See compose.yaml.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY . .
# Publishing for the image's own runtime identifier keeps native libraries (SkiaSharp for Kodi) to that platform.
RUN case "$TARGETARCH" in amd64) rid=linux-x64 ;; *) rid="linux-$TARGETARCH" ;; esac \
    && dotnet publish Neeo.Sdk.Server -c Release -r "$rid" --no-self-contained -o /app
# Each driver goes in its own folder: the server loads every driver into its own load context, resolving that
# driver's dependencies (and native libraries) from the driver's folder.
RUN case "$TARGETARCH" in amd64) rid=linux-x64 ;; *) rid="linux-$TARGETARCH" ;; esac \
    && for driver in Hisense Kodi Plex WebOS; do \
        dotnet publish "Neeo.Drivers.$driver" -c Release -r "$rid" --no-self-contained -o "/app/drivers/$driver" || exit 1; \
    done

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Registrations and their encryption key (settings.key) live here - mount a volume or the server forgets them on restart.
ENV NEEO_SETTINGS_PATH=/data
RUN mkdir -p /data && chown "$APP_UID" /data
VOLUME /data

# Semicolon-separated driver assemblies to load. Override to run only the drivers you want, e.g.
#   NEEO_Drivers=/app/drivers/Kodi/Neeo.Drivers.Kodi.dll;/app/drivers/WebOS/Neeo.Drivers.WebOS.dll
ENV NEEO_Drivers="/app/drivers/Hisense/Neeo.Drivers.Hisense.dll;/app/drivers/Kodi/Neeo.Drivers.Kodi.dll;/app/drivers/Plex/Neeo.Drivers.Plex.dll;/app/drivers/WebOS/Neeo.Drivers.WebOS.dll"

USER $APP_UID
ENTRYPOINT ["dotnet", "Neeo.Sdk.Server.dll"]
