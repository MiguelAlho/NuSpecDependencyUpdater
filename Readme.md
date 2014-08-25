This app is VERY opinionated. It builds upon zero29 conventions. It works through the working directory building a map of package projects (based on the presence of a nuspec file), assembly and package 
versions.

Once determined, all intra-solution dependencies in nuspec files should be updated so that the lowest version allowed is the current. This way an update on dependencies to the current version is possible.

This is more of a convention than a requirement, and will simplify dependency management by convention.

This is usefull to bulk update nupsec files, at the dependency tag, so that a package will attempt to get the most recent version of an assembly, especially if it is based on another VS Solution (when, for instance, a change has been made in upstream dependencies in a large project divided into multiple solutions).
