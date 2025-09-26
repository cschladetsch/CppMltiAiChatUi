#!/usr/bin/env python3
"""
Run script for MultipleAIApp
Usage: python r.py [arguments before --] -- [arguments to pass to app]

All arguments after the double-dash (--) are passed directly to the application.
"""

import subprocess
import sys
import os

def main():
    script_args = []
    app_args = []

    # Split arguments on '--'
    if '--' in sys.argv:
        dash_index = sys.argv.index('--')
        script_args = sys.argv[1:dash_index]
        app_args = sys.argv[dash_index + 1:]
    else:
        script_args = sys.argv[1:]
        app_args = []

    try:
        # Change to the directory containing the solution file
        script_dir = os.path.dirname(os.path.abspath(__file__))
        os.chdir(script_dir)

        # Build the dotnet run command
        cmd = ['dotnet', 'run', '--project', 'MultipleAIApp/MultipleAIApp.csproj', '--framework', 'net8.0-desktop']

        # Add script arguments (if any) before the -- separator for dotnet run
        if script_args:
            cmd.extend(script_args)

        # Add app arguments after -- for dotnet run
        if app_args:
            cmd.append('--')
            cmd.extend(app_args)

        print(f"Running: {' '.join(cmd)}")
        print("Application starting... (Press Ctrl+C to stop)")

        # Use Popen to get more control over the process
        process = subprocess.Popen(cmd)

        try:
            # Wait for the process to complete
            result = process.wait()
            if result != 0:
                print(f"Application exited with code {result}")
                sys.exit(result)
        except KeyboardInterrupt:
            print("\nStopping application...")
            process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                print("Force killing application...")
                process.kill()
            sys.exit(0)

    except subprocess.CalledProcessError as e:
        print(f"Command failed with exit code {e.returncode}")
        sys.exit(e.returncode)
    except KeyboardInterrupt:
        print("\nApplication interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()