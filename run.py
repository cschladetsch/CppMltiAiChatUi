#!/usr/bin/env python3
"""
Run script for CsharpUAI project
Handles running the various applications in the project
"""

import os
import sys
import subprocess
import argparse
from pathlib import Path


def run_command(cmd, cwd=None, check=True):
    """Run a command and handle errors"""
    print(f"Running: {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    try:
        result = subprocess.run(
            cmd,
            cwd=cwd,
            check=check,
            shell=True if isinstance(cmd, str) else False
        )
        return result
    except subprocess.CalledProcessError as e:
        print(f"Error running command: {e}")
        if check:
            sys.exit(1)
        return e


def run_application(project_name=None, configuration="Release", build_first=False):
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

        cmd = ["dotnet", "run", "--project", project_to_run]

        # Add configuration
        cmd.extend(["--configuration", configuration])

        # Add no-build flag if not building first
        if not build_first:
            cmd.append("--no-build")

        result = run_command(cmd, check=False)
        return result.returncode == 0
    else:
        print(f"Project {project_to_run} not found!")
        return False


def main():
    """Main run function"""
    parser = argparse.ArgumentParser(description="Run CsharpUAI applications")
    parser.add_argument("project", nargs="?", default="demo",
                       choices=["demo", "app"],
                       help="Project to run (default: demo)")
    parser.add_argument("--configuration", "-c", default="Release",
                       choices=["Debug", "Release"],
                       help="Build configuration (default: Release)")
    parser.add_argument("--build", action="store_true",
                       help="Build before running")

    args = parser.parse_args()

    print("=== CsharpUAI Run Script ===")

    # Change to project root directory
    project_root = Path(__file__).parent
    os.chdir(project_root)

    try:
        # Check .NET SDK
        result = run_command(["dotnet", "--version"], check=False)
        if result.returncode != 0:
            print("ERROR: .NET SDK not found. Please install .NET 8 SDK.")
            sys.exit(1)

        # Run application
        run_success = run_application(
            project_name=args.project,
            configuration=args.configuration,
            build_first=args.build
        )

        if not run_success:
            print("Failed to run application!")
            sys.exit(1)

    except KeyboardInterrupt:
        print("\nRun interrupted by user.")
        sys.exit(1)
    except Exception as e:
        print(f"Run failed: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()