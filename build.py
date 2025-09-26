#!/usr/bin/env python3
"""
Build script for CsharpUAI project
Supports building, cleaning, testing, and running the applications
"""

import os
import sys
import subprocess
import argparse
import shutil
from pathlib import Path


def run_command(cmd, cwd=None, check=True):
    """Run a command and handle errors"""
    print(f"Running: {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    try:
        result = subprocess.run(
            cmd,
            cwd=cwd,
            check=check,
            capture_output=True,
            text=True,
            shell=True if isinstance(cmd, str) else False
        )
        if result.stdout:
            print(result.stdout)
        if result.stderr and result.returncode == 0:
            print(result.stderr)
        return result
    except subprocess.CalledProcessError as e:
        print(f"Error running command: {e}")
        if e.stderr:
            print(f"stderr: {e.stderr}")
        if check:
            return e
        return e


def clean_project():
    """Clean build artifacts"""
    print("Cleaning project...")

    # Clean directories to remove
    clean_dirs = [
        "bin",
        "obj",
        "*/bin",
        "*/obj",
        "*/*/bin",
        "*/*/obj",
        "MultiLLM.Core/bin",
        "MultiLLM.Core/obj",
        "MultiLLM.Core.Tests/bin",
        "MultiLLM.Core.Tests/obj",
        "MultiLLM.Demo/bin",
        "MultiLLM.Demo/obj",
        "MultipleAIApp/MultipleAIApp/bin",
        "MultipleAIApp/MultipleAIApp/obj",
    ]

    for pattern in clean_dirs:
        for path in Path(".").glob(pattern):
            if path.is_dir():
                print(f"Removing {path}")
                shutil.rmtree(path, ignore_errors=True)

    # Clean with dotnet
    solutions = [
        "MultiLLM.sln",
        "MultipleAIApp/MultipleAIApp.sln"
    ]

    for solution in solutions:
        if os.path.exists(solution):
            print(f"Cleaning {solution}...")
            run_command(["dotnet", "clean", solution], check=False)

    print("Clean complete!")


def build_project(configuration="Release"):
    """Build the project"""
    print(f"Building project in {configuration} configuration...")

    solutions = [
        "MultiLLM.sln",
        "MultipleAIApp/MultipleAIApp.sln"
    ]

    build_success = True

    for solution in solutions:
        if os.path.exists(solution):
            print(f"Building {solution}...")
            result = run_command([
                "dotnet", "build", solution,
                "--configuration", configuration,
                "--no-restore"
            ], check=False)

            if result.returncode != 0:
                print(f"Build failed for {solution}")
                build_success = False
            else:
                print(f"Build successful for {solution}")
        else:
            print(f"Solution {solution} not found, skipping...")

    return build_success


def run_tests():
    """Run unit tests"""
    print("Running tests...")

    test_projects = [
        "MultiLLM.Core.Tests/MultiLLM.Core.Tests.csproj"
    ]

    test_success = True

    for test_project in test_projects:
        if os.path.exists(test_project):
            print(f"Running tests for {test_project}...")
            result = run_command([
                "dotnet", "test", test_project,
                "--no-build",
                "--verbosity", "normal"
            ], check=False)

            if result.returncode != 0:
                print(f"Tests failed for {test_project}")
                test_success = False
            else:
                print(f"Tests passed for {test_project}")
        else:
            print(f"Test project {test_project} not found, skipping...")

    return test_success


def run_application(project_name=None):
    """Run the application"""
    print("Running application...")

    # Determine which project to run
    runnable_projects = {
        "demo": "MultiLLM.Demo/MultiLLM.Demo.csproj",
        "app": "MultipleAIApp/MultipleAIApp/MultipleAIApp.csproj"
    }

    if project_name and project_name in runnable_projects:
        project_to_run = runnable_projects[project_name]
    else:
        # Default to demo if available, otherwise app
        if os.path.exists(runnable_projects["demo"]):
            project_to_run = runnable_projects["demo"]
            project_name = "demo"
        elif os.path.exists(runnable_projects["app"]):
            project_to_run = runnable_projects["app"]
            project_name = "app"
        else:
            print("No runnable projects found!")
            return False

    if os.path.exists(project_to_run):
        print(f"Running {project_name}: {project_to_run}")
        result = run_command([
            "dotnet", "run",
            "--project", project_to_run,
            "--no-build"
        ], check=False)

        return result.returncode == 0
    else:
        print(f"Project {project_to_run} not found!")
        return False


def main():
    """Main build function"""
    parser = argparse.ArgumentParser(description="Build CsharpUAI project")
    parser.add_argument("--clean", action="store_true",
                       help="Clean before building")
    parser.add_argument("--configuration", "-c", default="Release",
                       choices=["Debug", "Release"],
                       help="Build configuration (default: Release)")
    parser.add_argument("--test", action="store_true",
                       help="Run tests after building")
    parser.add_argument("--run", metavar="PROJECT",
                       nargs="?", const="",
                       help="Run application after building (demo/app, default: demo)")
    parser.add_argument("--skip-build", action="store_true",
                       help="Skip build step")

    args = parser.parse_args()

    print("=== CsharpUAI Build Script ===")

    # Change to project root directory
    project_root = Path(__file__).parent
    os.chdir(project_root)

    try:
        # Check .NET SDK
        result = run_command(["dotnet", "--version"], check=False)
        if result.returncode != 0:
            print("ERROR: .NET SDK not found. Please install .NET 8 SDK.")
            sys.exit(1)

        # Clean if requested
        if args.clean:
            clean_project()

        build_success = True
        test_success = True
        run_success = True

        # Build
        if not args.skip_build:
            build_success = build_project(args.configuration)
            if not build_success:
                print("Build failed!")
                if not args.run:  # If not running, exit on build failure
                    sys.exit(1)

        # Run tests if requested and build succeeded
        if args.test and build_success:
            test_success = run_tests()
            if not test_success:
                print("Tests failed!")
                if not args.run:  # If not running, exit on test failure
                    sys.exit(1)

        # Run application if requested and build succeeded
        if args.run is not None and build_success:
            project_name = args.run if args.run else None
            run_success = run_application(project_name)
            if not run_success:
                print("Failed to run application!")

        # Summary
        print("\n=== Build Summary ===")
        if not args.skip_build:
            print(f"Build: {'✓' if build_success else '✗'}")
        if args.test:
            print(f"Tests: {'✓' if test_success else '✗'}")
        if args.run is not None:
            print(f"Run: {'✓' if run_success else '✗'}")

        # Exit with appropriate code
        if not build_success or (args.test and not test_success):
            sys.exit(1)

    except KeyboardInterrupt:
        print("\nBuild interrupted by user.")
        sys.exit(1)
    except Exception as e:
        print(f"Build failed: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()